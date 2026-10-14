import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

async function module(name) {
    const source = await readFile(new URL(`../../../src/Frontend/WebApp/wwwroot/landing/js/${name}.js`, import.meta.url), 'utf8');
    return import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);
}

function target() {
    const listeners = new Map();
    return {
        addEventListener(type, fn) { listeners.set(type, fn); },
        removeEventListener(type, fn) { if (listeners.get(type) === fn) listeners.delete(type); },
        emit(type, event = {}) { listeners.get(type)?.(event); },
        get count() { return listeners.size; }
    };
}

test('carousel advances every four seconds and pauses for interaction, hidden pages and reduced motion', async () => {
    const { initialize } = await module('carousel');
    const motion = Object.assign(target(), { matches: false });
    globalThis.matchMedia = () => motion;
    globalThis.document = Object.assign(target(), { hidden: false });
    const element = Object.assign(target(), { contains: () => false });
    let callback, disconnected = false, next = 0;
    globalThis.IntersectionObserver = class { constructor(fn) { callback = fn; } observe() {} disconnect() { disconnected = true; } };
    const originalSet = globalThis.setInterval, originalClear = globalThis.clearInterval, timers = new Map();
    globalThis.setInterval = (fn, interval) => { timers.set(++next, { fn, interval }); return next; };
    globalThis.clearInterval = id => timers.delete(id);
    try {
        const calls = [];
        const instance = initialize(element, { invokeMethodAsync(name) { calls.push(name); return Promise.resolve(); } });
        callback([{ isIntersecting: true }]);
        assert.equal(timers.size, 1); const tick = [...timers.values()][0]; assert.equal(tick.interval, 4000);
        tick.fn(); assert.deepEqual(calls, ['AdvanceSlide']);
        element.emit('pointerenter'); assert.equal(timers.size, 0);
        element.emit('pointerleave'); assert.equal(timers.size, 1);
        element.emit('focusin'); assert.equal(timers.size, 0);
        element.emit('focusout', { relatedTarget: null }); assert.equal(timers.size, 1);
        document.hidden = true; document.emit('visibilitychange'); assert.equal(timers.size, 0);
        document.hidden = false; document.emit('visibilitychange'); assert.equal(timers.size, 1);
        motion.matches = true; motion.emit('change'); assert.equal(timers.size, 0);
        instance.dispose(); assert.ok(disconnected); assert.equal(element.count + document.count + motion.count, 0);
    } finally { globalThis.setInterval = originalSet; globalThis.clearInterval = originalClear; }
});

test('timeline follows scroll, ignores pointer movement, activates pins and preserves revealed steps', async () => {
    const { initialize } = await module('timeline');
    const motion = Object.assign(target(), { matches: false });
    globalThis.matchMedia = () => motion;
    const classes = () => { const set = new Set(); set.contains = value => set.has(value); set.remove = value => set.delete(value); return set; };
    const nodes = [0, 200, 400].map(offsetTop => {
        const pin = Object.assign(target(), { attributes: {}, setAttribute(key, value) { this.attributes[key] = value; } });
        return { offsetTop, offsetHeight: 100, classList: classes(), pin, querySelector: () => pin, getBoundingClientRect: () => ({ top: offsetTop, bottom: offsetTop + 100 }) };
    });
    const progress = { style: {} }, cursor = { style: {} };
    let trackTop = 800;
    const track = Object.assign(target(), { dataset: { revealState: 'visible' }, classList: classes(), offsetHeight: 500,
        querySelectorAll: () => nodes, querySelector: selector => selector.includes('progress') ? progress : cursor,
        getBoundingClientRect: () => ({ top: trackTop }) });
    globalThis.window = target(); globalThis.innerHeight = 800;
    let disconnected = false;
    globalThis.MutationObserver = class { observe() {} disconnect() { disconnected = true; } };
    const frames = new Map(); let next = 0;
    globalThis.requestAnimationFrame = fn => { frames.set(++next, fn); return next; };
    globalThis.cancelAnimationFrame = id => frames.delete(id);
    const instance = initialize(track);
    assert.ok(!nodes[0].classList.contains('is-revealed'));
    const flush = () => { for (let i = 0; i < 80 && frames.size; i++) { const [id, fn] = frames.entries().next().value; frames.delete(id); fn(); } };
    trackTop = 180; window.emit('scroll'); flush();
    assert.ok(Math.abs(parseFloat(progress.style.height) - 260) < 0.01);
    assert.ok(Math.abs(parseFloat(cursor.style.top) - 260) < 0.01);
    assert.ok(nodes[0].classList.contains('is-revealed')); assert.ok(nodes[1].classList.contains('is-revealed'));
    assert.ok(!nodes[2].classList.contains('is-revealed'));
    track.emit('pointermove', { pointerType: 'mouse', clientY: 500 }); flush();
    assert.ok(Math.abs(parseFloat(progress.style.height) - 260) < 0.01, 'Mouse movement must not move the timeline');
    trackTop = 340; window.emit('scroll'); flush();
    assert.ok(Math.abs(parseFloat(progress.style.height) - 100) < 0.01); assert.ok(nodes[1].classList.contains('is-revealed'));
    nodes[2].pin.emit('click'); assert.equal(nodes[2].pin.attributes['aria-expanded'], 'true');
    assert.ok(Math.abs(parseFloat(progress.style.height) - 100) < 0.01, 'Clicking a pin must not change the scroll-controlled position');
    instance.dispose(); assert.ok(disconnected); assert.equal(frames.size, 0);
    assert.equal(track.count + window.count + motion.count + nodes.reduce((sum, node) => sum + node.pin.count, 0), 0);
});

function revealFixture(reduced = false) {
    function element(tag, classes = [], children = []) {
        const classList = new Set(classes); classList.contains = name => classList.has(name);
        const node = {
            tag, classList, children, dataset: {},
            removeAttribute(name) { if (name === 'data-reveal-pending') delete this.dataset.revealPending; },
            matches(selector) {
                return selector.split(',').some(part => part === tag || (part.startsWith('.') && classList.has(part.slice(1))));
            },
            querySelector(selector) {
                for (const child of children) { if (child.matches(selector)) return child; const nested = child.querySelector(selector); if (nested) return nested; }
                return null;
            },
            querySelectorAll(selector) {
                return children.flatMap(child => [...(child.matches(selector) ? [child] : []), ...child.querySelectorAll(selector)]);
            }
        };
        children.forEach(child => child.parentElement = node); return node;
    }
    const heading = element('h2'), subtitle = element('p'), card = element('article', ['pricing-card'], [element('h3'), element('p')]);
    const grid = element('div', ['grid'], [card]);
    const section = element('section', [], [element('div', ['flex-col'], [element('div', [], [heading, subtitle]), grid])]);
    const root = element('div', [], [section]); root.dataset.revealPending = '';
    const motion = Object.assign(target(), { matches: reduced });
    globalThis.matchMedia = () => motion;
    globalThis.document = { querySelector: () => root, querySelectorAll: () => [] };
    const observers = [];
    globalThis.IntersectionObserver = class {
        constructor(callback, options) { this.callback = callback; this.options = options; this.observed = new Set(); observers.push(this); }
        observe(node) { this.observed.add(node); }
        unobserve(node) { this.observed.delete(node); }
        disconnect() { this.observed.clear(); this.disconnected = true; }
        enter(node) { this.callback([{ target: node, isIntersecting: true }]); }
    };
    return { root, heading, subtitle, card, motion, observers };
}

test('scroll reveal waits for the title before revealing content, then reveals only once', async () => {
    const { initializeScrollReveal } = await module('scroll-reveal');
    const { root, heading, subtitle, card, observers } = revealFixture();
    const instance = initializeScrollReveal(root);
    assert.equal(root.dataset.revealPending, undefined);
    assert.equal(heading.dataset.revealState, 'pending'); assert.equal(card.dataset.revealState, 'pending');
    const [titles, content] = observers;
    assert.notEqual(titles.options.rootMargin, content.options.rootMargin);
    content.enter(subtitle); content.enter(card);
    await new Promise(resolve => setTimeout(resolve, 20));
    assert.equal(subtitle.dataset.revealState, 'pending', 'Content must wait even if its observer fires first');
    titles.enter(heading); assert.equal(heading.dataset.revealState, 'visible');
    assert.equal(subtitle.dataset.revealState, 'pending');
    await new Promise(resolve => setTimeout(resolve, 340));
    assert.equal(subtitle.dataset.revealState, 'visible'); assert.equal(card.dataset.revealState, 'visible');
    assert.equal(titles.observed.size + content.observed.size, 0);
    instance.dispose(); assert.equal(heading.dataset.scrollReveal, undefined);
});

test('reduced motion reveals all content immediately, including changes during a reveal', async () => {
    const { initializeScrollReveal } = await module('scroll-reveal');
    const first = revealFixture(true), instance = initializeScrollReveal(first.root);
    assert.equal(first.observers.length, 0); assert.equal(first.card.dataset.revealState, 'visible'); instance.dispose();
    const second = revealFixture(), pending = initializeScrollReveal(second.root);
    second.motion.matches = true; second.motion.emit('change');
    assert.equal(second.heading.dataset.revealState, 'visible'); assert.equal(second.card.dataset.revealState, 'visible');
    assert.ok(second.observers.every(observer => observer.disconnected));
    pending.dispose(); assert.equal(second.motion.count, 0);
});

test('disposing scroll reveal cancels delayed content and removes observers', async () => {
    const { initializeScrollReveal } = await module('scroll-reveal');
    const { root, heading, card, motion, observers } = revealFixture();
    const instance = initializeScrollReveal(root);
    observers[0].enter(heading); observers[1].enter(card); instance.dispose();
    await new Promise(resolve => setTimeout(resolve, 300));
    assert.equal(card.dataset.revealState, undefined);
    assert.ok(observers.every(observer => observer.disconnected)); assert.equal(motion.count, 0);
});

test('particles respect DPR, visibility and reduced motion, and release resources', async () => {
    const motion = Object.assign(target(), { matches: false });
    globalThis.window = target();
    globalThis.document = Object.assign(target(), { hidden: false });
    globalThis.matchMedia = () => motion;
    globalThis.devicePixelRatio = 3;
    const frames = new Map(); let next = 0, disconnected = false, resize;
    globalThis.requestAnimationFrame = fn => { frames.set(++next, fn); return next; };
    globalThis.cancelAnimationFrame = id => frames.delete(id);
    globalThis.ResizeObserver = class {
        constructor(fn) { resize = fn; }
        observe() {}
        disconnect() { disconnected = true; }
    };
    let painted = 0; const points = [];
    const ctx = { setTransform() {}, clearRect() { painted++; points.length = 0; }, beginPath() {}, arc(x, y) { points.push({ x, y }); }, fill() {}, moveTo() {}, lineTo() {}, stroke() {} };
    const canvas = { clientWidth: 390, clientHeight: 844, getContext: () => ctx };
    const { initialize } = await module('particles');
    const instance = initialize(canvas);
    assert.equal(canvas.width, 780); assert.equal(canvas.height, 1688);
    assert.equal(frames.size, 1);
    const initial = { ...points[0] };
    window.emit('pointermove', { pointerType: 'mouse', clientX: initial.x + 100, clientY: initial.y });
    const [id, tick] = frames.entries().next().value; frames.delete(id); tick();
    assert.ok(points[0].x > initial.x, 'Particle should move toward the pointer');
    const displaced = points[0].x;
    document.emit('pointerleave');
    function step() {
        const [frameId, callback] = frames.entries().next().value;
        frames.delete(frameId); callback();
    }
    step();
    assert.ok(points[0].x < displaced && points[0].x > initial.x, 'Return should start without snapping home');
    assert.ok(Math.abs(points[0].x - displaced) < Math.abs(displaced - initial.x) * 0.01, 'First return step should be slow');
    for (let i = 0; i < 1000; i++) step();
    assert.ok(Math.abs(points[0].x - initial.x) < Math.abs(displaced - initial.x) * 0.01, 'Particle should return to its fixed original position');
    document.hidden = true; document.emit('visibilitychange'); assert.equal(frames.size, 0);
    document.hidden = false; document.emit('visibilitychange'); assert.equal(frames.size, 1);
    motion.matches = true; motion.emit('change'); assert.equal(frames.size, 0);
    assert.ok(painted > 1);
    canvas.clientWidth = 768; resize(); assert.equal(canvas.width, 1536);
    instance.dispose(); assert.ok(disconnected);
    assert.equal(window.count + document.count + motion.count, 0);
    assert.equal(frames.size, 0);
    const staticInstance = initialize(canvas); assert.equal(frames.size, 0); staticInstance.dispose();
});

test('mouse connections reach full brightness nearby and fade with distance', async () => {
    const { connectionStrength } = await module('particles');
    assert.equal(connectionStrength(0), 1);
    assert.equal(connectionStrength(45), 1);
    assert.ok(connectionStrength(100) > connectionStrength(200));
    assert.ok(connectionStrength(200) > connectionStrength(299));
    assert.equal(connectionStrength(300), 0);
    assert.equal(connectionStrength(400), 0);
});

test('neon topic line stretches its leading edge before bringing the trailing edge across', async () => {
    const { lineFrames } = await module('feature-line');
    const left = { left: 0, top: 200, width: 250 }, right = { left: 300, top: 200, width: 250 };
    const forward = lineFrames(left, right);
    assert.equal(forward[1].left, '0px'); assert.equal(forward[1].width, '550px');
    assert.equal(forward[2].left, '300px'); assert.equal(forward[2].width, '250px');
    const reverse = lineFrames(right, left);
    assert.equal(reverse[0].left, '300px'); assert.equal(reverse[1].left, '0px');
    assert.equal(reverse[1].width, '550px'); assert.equal(reverse[2].width, '250px');
});

test('mobile menu closes on Escape and desktop resize, without stealing focus when closed', async () => {
    const desktop = Object.assign(target(), { matches: false });
    globalThis.matchMedia = () => desktop;
    let expanded = true, focused = 0;
    const toggle = { getAttribute: () => String(expanded), focus() { focused++; } };
    globalThis.document = Object.assign(target(), { getElementById: () => toggle });
    const calls = [];
    const { initialize } = await module('viewport');
    const instance = initialize({ invokeMethodAsync(name) { calls.push(name); return Promise.resolve(); } });
    document.emit('keydown', { key: 'Escape' }); assert.equal(calls.length, 1); assert.equal(focused, 1);
    expanded = false; document.emit('keydown', { key: 'Escape' }); assert.equal(calls.length, 1);
    desktop.matches = true; desktop.emit('change'); assert.equal(calls.length, 2);
    instance.dispose(); assert.equal(document.count + desktop.count, 0);
});

test('scroll spy only notifies section changes and disconnects observers', async () => {
    globalThis.window = target();
    const positions = { hero: 0, 'como-funciona': 600, funil: 900, recursos: 1500, 'para-quem': 2000, faq: 2500, planos: 3000 };
    globalThis.document = { getElementById(id) { return id === 'main-content' ? {} : { getBoundingClientRect: () => ({ top: positions[id] }) }; } };
    let update, disconnects = 0;
    globalThis.IntersectionObserver = class { constructor(fn) { update = fn; } observe() {} disconnect() { disconnects++; } };
    globalThis.MutationObserver = class { observe() {} disconnect() { disconnects++; } };
    const calls = [];
    const { initialize } = await module('scroll-spy');
    const instance = initialize({ invokeMethodAsync(name, id) { calls.push(id); return Promise.resolve(); } });
    update(); assert.deepEqual(calls, ['hero']);
    positions.hero = -900; positions['como-funciona'] = -300; positions.recursos = 100;
    update(); update(); assert.deepEqual(calls, ['hero', 'recursos']);
    instance.dispose(); assert.equal(disconnects, 2); assert.equal(window.count, 0);
});

test('scroll spy handles a missing main-content element without breaking page initialization', async () => {
    globalThis.window = target();
    globalThis.document = { getElementById: () => null };
    let observes = 0, disconnects = 0;
    globalThis.IntersectionObserver = class { observe() {} disconnect() { disconnects++; } };
    globalThis.MutationObserver = class {
        observe(element) { assert.ok(element, 'MutationObserver must never receive null'); observes++; }
        disconnect() { disconnects++; }
    };
    const calls = [], { initialize } = await module('scroll-spy');
    const instance = initialize({ invokeMethodAsync(name, id) { calls.push(id); return Promise.resolve(); } });
    assert.equal(observes, 0); assert.deepEqual(calls, ['hero']);
    instance.dispose(); assert.equal(disconnects, 2); assert.equal(window.count, 0);
});
