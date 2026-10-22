#!/usr/bin/env python3
"""Build pinned Meta v26 parameter metadata without importing or executing SDK code.

Usage: python3 scripts/meta/build-resource-catalog.py SDK_ARCHIVE OUTPUT_JSON
The archive must be the official GitHub source archive for PIN below.
"""
import argparse
import ast,json,zipfile
from pathlib import Path

PIN='efd8423a2e595ea8d4c04eb824ce113f2f1d68cd'
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('archive', type=Path)
parser.add_argument('output', type=Path)
args = parser.parse_args()
archive=zipfile.ZipFile(args.archive)
if not all(p.startswith('facebook-python-business-sdk-' + PIN + '/') for p in archive.namelist()):
 raise SystemExit('Archive root does not match the pinned official SDK commit.')
files={p.rsplit('/',1)[-1][:-3]:p for p in archive.namelist() if '/facebook_business/adobjects/' in p and p.endswith('.py')}
cache={}
def cls(name):
 key=name.lower()
 if key not in files:return None
 if key not in cache:
  mod=ast.parse(archive.read(files[key]).decode())
  cache[key]=next((n for n in mod.body if isinstance(n,ast.ClassDef)),None)
 return cache[key]
def assignment(node,name):
 for n in node.body:
  if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id==name for t in n.targets):return n.value
 return None
def enum_values(c,enum):
 nested=next((n for n in c.body if isinstance(n,ast.ClassDef) and n.name==enum),None) if c else None
 if not nested:return []
 return [n.value.value for n in nested.body if isinstance(n,ast.Assign) and isinstance(n.value,ast.Constant) and isinstance(n.value.value,str)]
def operation(c,method):
 fun=next(n for n in c.body if isinstance(n,ast.FunctionDef) and n.name==method)
 params=ast.literal_eval(assignment(fun,'param_types'))
 enums={}
 e=assignment(fun,'enums')
 for k,v in zip(e.keys,e.values):
  if isinstance(v,ast.List):enums[k.value]=ast.literal_eval(v)
  elif isinstance(v,ast.Call):
   ref=v.func.value.value
   if isinstance(ref,ast.Attribute) and isinstance(ref.value,ast.Name):enums[k.value]=enum_values(cls(ref.value.id),ref.attr)
 return params,enums

labels={
 'name':'Nome','objective':'Objetivo da campanha','buying_type':'Tipo de compra','status':'Status configurado','campaign_id':'Campanha','adset_id':'Conjunto de anúncios',
 'daily_budget':'Orçamento diário','lifetime_budget':'Orçamento vitalício','spend_cap':'Limite de gastos','bid_strategy':'Estratégia de lance','bid_amount':'Valor do lance','bid_constraints':'Restrições de lance',
 'budget_rebalance_flag':'Redistribuição de orçamento','budget_schedule_specs':'Programação de aumentos de orçamento','is_budget_schedule_enabled':'Ativar programação de orçamento','is_adset_budget_sharing_enabled':'Compartilhar orçamento entre conjuntos',
 'special_ad_categories':'Categorias de anúncio especiais','special_ad_category':'Categoria especial (legado)','special_ad_category_country':'Países das categorias especiais','promoted_object':'Objeto promovido: pixel, aplicativo ou catálogo',
 'targeting':'Segmentação do público e posicionamentos','optimization_goal':'Meta de otimização','optimization_sub_event':'Subevento de otimização','billing_event':'Evento de cobrança','destination_type':'Tipo de destino',
 'start_time':'Início da veiculação','end_time':'Fim da veiculação','stop_time':'Encerramento da campanha','ad_schedule_start_time':'Início programado do anúncio','ad_schedule_end_time':'Fim programado do anúncio',
 'adset_schedule':'Programação por dias e horários','attribution_spec':'Janelas e eventos de atribuição','attribution_count_type':'Contagem de atribuição','campaign_attribution':'Atribuição da campanha',
 'dsa_beneficiary':'Beneficiário do anúncio (DSA)','dsa_payor':'Pagador do anúncio (DSA)','regional_regulated_categories':'Categorias reguladas por região','regional_regulation_identities':'Identidades para regras regionais',
 'is_dynamic_creative':'Criativo dinâmico','creative':'Criativo do anúncio','creative_id':'ID do criativo autorizado','tracking_specs':'Rastreamento do anúncio','conversion_domain':'Domínio de conversão','priority':'Prioridade','display_sequence':'Ordem de exibição',
 'adlabels':'Rótulos de organização','pacing_type':'Ritmo de entrega','frequency_control_specs':'Controle de frequência','iterative_split_test_configs':'Configurações de teste dividido','migrate_to_advantage_plus':'Migrar para Advantage+',
 'smart_promotion_type':'Tipo de promoção inteligente','is_skadnetwork_attribution':'Atribuição SKAdNetwork','is_incremental_attribution_enabled':'Atribuição incremental','value_rules_applied':'Aplicar regras de valor','value_rule_set_id':'Conjunto de regras de valor',
 'existing_customer_budget_percentage':'Orçamento para clientes existentes (%)','min_budget_spend_percentage':'Participação mínima do orçamento (%)','max_budget_spend_percentage':'Participação máxima do orçamento (%)',
 'daily_min_spend_target':'Gasto mínimo diário','daily_spend_cap':'Limite diário de gastos','lifetime_min_spend_target':'Gasto mínimo vitalício','lifetime_spend_cap':'Limite vitalício de gastos','budget_source':'Origem do orçamento',
 'creative_automation_spec':'Automação do criativo','creative_asset_groups_spec':'Grupos de ativos do criativo','creative_audience_pairing_persona':'Combinação de criativo e perfil de público','dataset_split_specs':'Divisão de conjuntos de dados',
 'object_story_spec':'Página, identidade e conteúdo do criativo','asset_feed_spec':'Ativos dinâmicos: imagens, vídeos, textos e links','degrees_of_freedom_spec':'Aprimoramentos criativos Advantage+','object_story_id':'Publicação existente','product_set_id':'Conjunto de produtos do catálogo',
 'page_id':'Página do Facebook','instagram_user_id':'Identidade do Instagram','image_hash':'Hash da imagem enviada','image_url':'URL da imagem','image_file':'Arquivo de imagem','video_id':'Vídeo enviado','thumbnail_url':'URL da miniatura',
 'body':'Texto principal','title':'Título','link_url':'URL do link','url_tags':'Parâmetros de rastreamento','call_to_action':'Chamada para ação','generative_asset_spec':'Ativos criados com IA','media_optimization_spec':'Otimização de mídia','media_sourcing_spec':'Origem da mídia',
 'geo_locations':'Localizações incluídas','excluded_geo_locations':'Localizações excluídas','countries':'Países','cities':'Cidades','regions':'Regiões','zips':'Códigos postais','custom_locations':'Localizações personalizadas',
 'age_min':'Idade mínima','age_max':'Idade máxima','genders':'Gêneros','locales':'Idiomas','interests':'Interesses','behaviors':'Comportamentos','custom_audiences':'Públicos personalizados','excluded_custom_audiences':'Públicos excluídos','exclusions':'Exclusões de público','flexible_spec':'Combinações de interesses e públicos',
 'publisher_platforms':'Plataformas de veiculação','facebook_positions':'Posicionamentos no Facebook','instagram_positions':'Posicionamentos no Instagram','messenger_positions':'Posicionamentos no Messenger','audience_network_positions':'Posicionamentos na Audience Network','threads_positions':'Posicionamentos no Threads','whatsapp_positions':'Posicionamentos no WhatsApp','device_platforms':'Dispositivos','user_os':'Sistemas operacionais','user_device':'Modelos de dispositivo','targeting_automation':'Expansão de público Advantage+',
 'pixel_id':'Pixel','application_id':'Aplicativo','object_store_url':'URL da loja de aplicativos','custom_event_type':'Evento de conversão','custom_conversion_id':'Conversão personalizada','product_catalog_id':'Catálogo de produtos','event_type':'Tipo de evento','window_days':'Janela em dias',
 'link_data':'Conteúdo do anúncio com link','video_data':'Conteúdo do anúncio em vídeo','photo_data':'Conteúdo do anúncio com foto','template_data':'Modelo de anúncio de catálogo','message':'Texto principal','link':'URL de destino','description':'Descrição','image_crops':'Recortes da imagem','child_attachments':'Cartões do carrossel',
 'source_campaign_id':'Campanha de origem','source_adset_id':'Conjunto de origem','source_ad_id':'Anúncio de origem','account_id':'Conta proprietária','execution_options':'Opções de execução da API','adset_spec':'Criação embutida de conjunto','campaign_spec':'Criação embutida de campanha'
}
restrictions={
 'account_id':'A conta é fixada pelo vínculo autorizado do workspace; não pode ser transferida por este campo.',
 'execution_options':'Reservado ao transporte. Este fluxo confirma gravações reais por diário idempotente; validate_only não é uma gravação.',
 'campaign_spec':'Crie a campanha no formulário dedicado e selecione seu ID; criação embutida não é utilizada.',
 'adset_spec':'Crie o conjunto no formulário dedicado e selecione seu ID; criação embutida não é utilizada.',
 'image_file':'Envie o arquivo pelo botão de mídia e utilize image_hash. Caminhos locais do servidor não são aceitos.',
 'is_dco_internal':'Parâmetro interno da Meta, não apresentado como configuração pública garantida.',
 'draft_adgroup_id':'Fluxo de rascunhos da Meta não implementado; utilize o anúncio pausado e seu ID confirmado.'
}
overrides={'promoted_object':'AdPromotedObject','asset_feed_spec':'AdAssetFeedSpec','frequency_control_specs':'list<AdCampaignFrequencyControlSpecs>','attribution_spec':'list<AttributionSpec>'}
def group(name):
 if name in ['name','objective','buying_type','campaign_id','adset_id','creative']:return 'Detalhes e vínculo'
 if any(x in name for x in ['special_ad','dsa_','regulat','authorization','disclaimer']):return 'Categorias e transparência'
 if any(x in name for x in ['budget','spend','bid_','pacing','daily_imps','lifetime_imps']):return 'Orçamento e lances'
 if any(x in name for x in ['start','stop','end_time','schedule','rotation','delivery','status']):return 'Programação e entrega'
 if any(x in name for x in ['targeting','audience','attribution','destination','conversion','optimization','promoted']):return 'Público, destino e conversões'
 if any(x in name for x in ['test','frequency']):return 'Experimentos e frequência'
 if any(x in name for x in ['creative','asset','story','image','video','body','title','link','instagram','actor','object_','media','product','template','call_to']):return 'Criativo, identidade e catálogo'
 if name.startswith('is_') or 'smart' in name or 'advantage' in name:return 'Advantage+ e automação'
 return 'Organização e opções avançadas'
def nested(type_,name,depth=0,seen=()):
 if depth>=3:return []
 actual=overrides.get(name,type_)
 while actual.startswith('list<'):actual=actual[5:-1]
 if actual in seen:return []
 c=cls(actual)
 if not c:return []
 fields=assignment(c,'_field_types')
 if fields is None:return []
 fields=ast.literal_eval(fields)
 result=[]
 for key,kind in fields.items():
  enums=enum_values(c,kind.removeprefix('list<').removesuffix('>'))
  restriction='Campo efetivo de leitura, calculado pela Meta.' if key.startswith('effective_') else None
  result.append(field(key,kind,True,True,True,enums,restriction,depth+1,seen+(actual,)))
 return result
def field(name,type_,create,update,readable,enums,restriction=None,depth=0,seen=()):
 if name=='objective':enums=[v for v in enums if v.startswith('OUTCOME_')]
 children=[] if restriction else nested(type_,name,depth,seen)
 if name=='creative':children=[field('creative_id','string',True,True,True,[],depth=3)]
 desc='Opção da API pública v26. A Meta valida compatibilidade com objetivo, ativos, região, permissões e elegibilidade da conta.'
 if name=='targeting':desc='Localização, idade, idiomas, públicos e posicionamentos. Preserve regras não alteradas; categorias especiais restringem segmentação.'
 if name=='promoted_object':desc='Referencie pixel/evento, aplicativo, catálogo ou conjunto de produtos elegível para o objetivo.'
 if name in ['daily_budget','lifetime_budget','spend_cap','bid_amount']:desc='Valor monetário em unidades mínimas da conta. Não combine orçamento diário e vitalício positivos.'
 if name=='iterative_split_test_configs':desc='Especificação de teste dividido disponível neste endpoint; experimentos A/B completos são recursos separados da Meta.'
 return dict(Name=name,Label=labels.get(name,name.replace('_',' ').capitalize()),Group=group(name),Description=desc,Type=type_,Create=create,Update=update,Readable=readable,EnumValues=sorted(set(enums)),Restriction=restriction,Children=children)
resources=[]
for name,resource,method in [('campaign','campaigns','create_campaign'),('adset','adsets','create_ad_set'),('ad','ads','create_ad'),('adcreative','adcreatives','create_ad_creative')]:
 create,ce=operation(cls('AdAccount'),method);update,ue=operation(cls(name),'api_update')
 readable=ast.literal_eval(assignment(cls(name),'_field_types'))
 fields=[]
 for key in sorted(create.keys()|update.keys()):
  t=create.get(key,update.get(key));enum_type=t.removeprefix('list<').removesuffix('>')
  enums=ce.get(enum_type,[])+ue.get(enum_type,[])
  fields.append(field(key,t,key in create,key in update,key in readable,enums,restrictions.get(key)))
 resources.append(dict(Resource=resource,ApiVersion='v26.0',SdkVersion='26.0.2',SourceCommit=PIN,Fields=fields))
out=args.output;out.write_text(json.dumps(resources,ensure_ascii=False,indent=2))
for r in resources:print(r['Resource'],len(r['Fields']),'fields',sum(f['Create'] for f in r['Fields']),'create',sum(f['Update'] for f in r['Fields']),'update')
print('Catalog bytes',out.stat().st_size)
