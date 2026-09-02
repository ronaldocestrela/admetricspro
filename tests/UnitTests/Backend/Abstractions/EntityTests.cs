using BuildingBlocks.Domain.Abstractions;
using FluentAssertions;

namespace UnitTests.Backend.Abstractions;

/// <summary>
/// Unit tests for <see cref="Entity{TId}"/>.
/// </summary>
public sealed class EntityTests
{
    private sealed class SampleEntity : Entity<Guid>
    {
        public SampleEntity(Guid id, string name)
            : base(id)
        {
            Name = name;
        }

        public string Name { get; }
    }

    private sealed class AnotherEntity : Entity<Guid>
    {
        public AnotherEntity(Guid id)
            : base(id)
        {
        }
    }

    /// <summary>
    /// Verifies constructor sets Id.
    /// </summary>
    [Fact]
    public void Constructor_ShouldSetIdCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new SampleEntity(id, "Test");

        // Assert
        entity.Id.Should().Be(id);
    }

    /// <summary>
    /// Verifies equality by Id and type.
    /// </summary>
    [Fact]
    public void Equals_ShouldReturnTrue_WhenEntitiesHaveSameIdAndType()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new SampleEntity(id, "First");
        var entity2 = new SampleEntity(id, "Second");

        // Act & Assert
        entity1.Equals(entity2).Should().BeTrue();
        (entity1 == entity2).Should().BeTrue();
        (entity1 != entity2).Should().BeFalse();
    }

    /// <summary>
    /// Verifies inequality for different Ids.
    /// </summary>
    [Fact]
    public void Equals_ShouldReturnFalse_WhenEntitiesHaveDifferentIds()
    {
        // Arrange
        var entity1 = new SampleEntity(Guid.NewGuid(), "Sample");
        var entity2 = new SampleEntity(Guid.NewGuid(), "Sample");

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
        (entity1 == entity2).Should().BeFalse();
        (entity1 != entity2).Should().BeTrue();
    }

    /// <summary>
    /// Verifies inequality when compared with null.
    /// </summary>
    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparedWithNull()
    {
        // Arrange
        var entity = new SampleEntity(Guid.NewGuid(), "Sample");

        // Act & Assert
        entity.Equals(null).Should().BeFalse();
        (entity == null).Should().BeFalse();
        (null == entity).Should().BeFalse();
        (entity != null).Should().BeTrue();
        (null != entity).Should().BeTrue();
    }

    /// <summary>
    /// Verifies null compared to null.
    /// </summary>
    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingNullWithNull()
    {
        SampleEntity? nullEntity1 = null;
        SampleEntity? nullEntity2 = null;

        (nullEntity1 == nullEntity2).Should().BeTrue();
        (nullEntity1 != nullEntity2).Should().BeFalse();
    }

    /// <summary>
    /// Verifies inequality for different entity types.
    /// </summary>
    [Fact]
    public void Equals_ShouldReturnFalse_WhenTypesAreDifferent_EvenWithSameId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sample = new SampleEntity(id, "Sample");
        var another = new AnotherEntity(id);

        // Act & Assert
        sample.Equals(another).Should().BeFalse();
    }

    /// <summary>
    /// Verifies hashcode consistency.
    /// </summary>
    [Fact]
    public void GetHashCode_ShouldReturnSameHashCode_WhenEntitiesHaveSameIdAndType()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity1 = new SampleEntity(id, "First");
        var entity2 = new SampleEntity(id, "Second");

        // Act & Assert
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }
}
