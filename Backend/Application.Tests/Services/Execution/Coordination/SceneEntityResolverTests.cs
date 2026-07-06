using System.Reactive.Subjects;
using FHOOE.Freydis.Application.Services.EntityManagement.PositionTags;
using FHOOE.Freydis.Application.Services.EntityManagement.SceneObjects;
using FHOOE.Freydis.Application.Services.Execution.Coordination;
using FHOOE.Freydis.Domain.Entities.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FHOOE.Freydis.Application.Tests.Services.Execution.Coordination;

/// <summary>
///     Tests for <see cref="SceneEntityResolver" />, verifying that PositionTag and SceneObject
///     skill properties are refreshed from observable-backed caches at execution time.
/// </summary>
public sealed class SceneEntityResolverTests : IDisposable
{
    private static readonly Guid TagId = Guid.Parse("a1c04473-0bea-43d1-8bc2-3406bcd4361c");
    private static readonly Guid ObjId = Guid.Parse("b2d05584-1cfb-54e2-9cd3-4517def5472d");

    private readonly Subject<IReadOnlyList<PositionTag>> _tagSubject = new();
    private readonly Subject<IReadOnlyList<SceneObject>> _objSubject = new();
    private readonly SceneEntityResolver _resolver;

    public SceneEntityResolverTests()
    {
        var mockTagService = new Mock<IPositionTagApplicationService>();
        mockTagService.Setup(s => s.GetAllPositionTagsAsync())
            .ReturnsAsync(new List<PositionTag>
            {
                MakeTag(TagId, "Tag1", 1.0, 2.0, 3.0)
            });
        mockTagService.Setup(s => s.OnPositionTagsChanged())
            .Returns(_tagSubject);

        var mockObjService = new Mock<ISceneObjectApplicationService>();
        mockObjService.Setup(s => s.GetAllSceneObjectsAsync())
            .ReturnsAsync(new List<SceneObject>
            {
                MakeObj(ObjId, "Obj1", 10.0, 20.0, 30.0)
            });
        mockObjService.Setup(s => s.OnSceneObjectsChanged())
            .Returns(_objSubject);

        _resolver = new SceneEntityResolver(
            mockTagService.Object,
            mockObjService.Object,
            NullLogger<SceneEntityResolver>.Instance);
    }

    public void Dispose()
    {
        _resolver.Dispose();
        _tagSubject.Dispose();
        _objSubject.Dispose();
    }

    [Fact]
    public void RefreshPositionTag_UpdatesStalePosition()
    {
        var staleTag = MakeTag(TagId, "Tag1", 0.0, 0.0, 0.0); // stale position
        var skill = MakeSkillWithTag(staleTag);

        var refreshed = _resolver.RefreshSceneEntityProperties(skill);

        var prop = refreshed.Properties[0];
        var tag = (PositionTag)prop.Value.Value!;
        Assert.Equal(1.0, tag.Position.X);
        Assert.Equal(2.0, tag.Position.Y);
        Assert.Equal(3.0, tag.Position.Z);
    }

    [Fact]
    public void RefreshPositionTag_UnknownId_LeavesUnchanged()
    {
        var unknownTag = MakeTag(Guid.NewGuid(), "Unknown", 0.0, 0.0, 0.0);
        var skill = MakeSkillWithTag(unknownTag);

        var refreshed = _resolver.RefreshSceneEntityProperties(skill);

        Assert.Same(skill, refreshed); // same instance, no change
    }

    [Fact]
    public void RefreshPositionTag_AlreadyFresh_ReturnsUnchanged()
    {
        var freshTag = MakeTag(TagId, "Tag1", 1.0, 2.0, 3.0); // matches cache
        var skill = MakeSkillWithTag(freshTag);

        var refreshed = _resolver.RefreshSceneEntityProperties(skill);

        Assert.Same(skill, refreshed);
    }

    [Fact]
    public void RefreshSceneObject_UpdatesStaleObject()
    {
        var staleObj = MakeObj(ObjId, "Obj1", 0.0, 0.0, 0.0);
        var skill = MakeSkillWithObj(staleObj);

        var refreshed = _resolver.RefreshSceneEntityProperties(skill);

        var prop = refreshed.Properties[0];
        var obj = (SceneObject)prop.Value.Value!;
        Assert.Equal(10.0, obj.Position.X);
        Assert.Equal(20.0, obj.Position.Y);
        Assert.Equal(30.0, obj.Position.Z);
    }

    [Fact]
    public void RefreshMixedProperties_OnlyRefreshesEntityTypes()
    {
        var staleTag = MakeTag(TagId, "Tag1", 0.0, 0.0, 0.0);
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Mixed",
            Description = "Mixed property skill",
            Properties =
            [
                new TypedProperty
                    { Name = "Duration", Value = TypedValue.Number(5.0), Direction = PropertyDirection.Input },
                new TypedProperty
                    { Name = "Target", Value = TypedValue.PositionTag(staleTag), Direction = PropertyDirection.Input }
            ]
        };

        var refreshed = _resolver.RefreshSceneEntityProperties(skill);

        // Number property unchanged
        Assert.Equal(5.0, refreshed.Properties[0].Value.Value);
        // PositionTag refreshed
        var tag = (PositionTag)refreshed.Properties[1].Value.Value!;
        Assert.Equal(1.0, tag.Position.X);
    }

    [Fact]
    public void ObservableUpdate_RefreshesCacheImmediately()
    {
        // Emit updated tag via observable
        var updatedTag = MakeTag(TagId, "Tag1", 99.0, 88.0, 77.0);
        _tagSubject.OnNext(new List<PositionTag> { updatedTag });

        var staleTag = MakeTag(TagId, "Tag1", 0.0, 0.0, 0.0);
        var skill = MakeSkillWithTag(staleTag);

        var refreshed = _resolver.RefreshSceneEntityProperties(skill);

        var tag = (PositionTag)refreshed.Properties[0].Value.Value!;
        Assert.Equal(99.0, tag.Position.X);
        Assert.Equal(88.0, tag.Position.Y);
        Assert.Equal(77.0, tag.Position.Z);
    }

    [Fact]
    public void ObservableDelete_RemovesFromCache()
    {
        // Emit empty list — tag no longer exists
        _tagSubject.OnNext(new List<PositionTag>());

        var staleTag = MakeTag(TagId, "Tag1", 0.0, 0.0, 0.0);
        var skill = MakeSkillWithTag(staleTag);

        var refreshed = _resolver.RefreshSceneEntityProperties(skill);

        // Tag not in cache → left unchanged (same instance)
        Assert.Same(skill, refreshed);
    }

    [Fact]
    public void NoEntityProperties_ReturnsSkillUnchanged()
    {
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "Simple",
            Description = "Simple property skill",
            Properties =
            [
                new TypedProperty
                    { Name = "Speed", Value = TypedValue.Number(1.5), Direction = PropertyDirection.Input },
                new TypedProperty
                    { Name = "Label", Value = TypedValue.Text("hello"), Direction = PropertyDirection.Input }
            ]
        };

        var refreshed = _resolver.RefreshSceneEntityProperties(skill);

        Assert.Same(skill, refreshed);
    }

    // ─── Helpers ──────────────────────────────────────────────────────

    private static PositionTag MakeTag(Guid id, string name, double x, double y, double z)
    {
        return new PositionTag
        {
            Id = id,
            Tag = name,
            Position = new Position { X = x, Y = y, Z = z }
        };
    }

    private static SceneObject MakeObj(Guid id, string name, double x, double y, double z)
    {
        return new SceneObject
        {
            Id = id,
            Name = name,
            Position = new Position { X = x, Y = y, Z = z }
        };
    }

    private static Skill MakeSkillWithTag(PositionTag tag)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = "MoveToTag",
            Description = "Move to position tag",
            Properties =
            [
                new TypedProperty
                    { Name = "Target", Value = TypedValue.PositionTag(tag), Direction = PropertyDirection.Input }
            ]
        };
    }

    private static Skill MakeSkillWithObj(SceneObject obj)
    {
        return new Skill
        {
            Id = Guid.NewGuid(),
            Name = "InspectObj",
            Description = "Inspect scene object",
            Properties =
            [
                new TypedProperty
                    { Name = "Target", Value = TypedValue.SceneObject(obj), Direction = PropertyDirection.Input }
            ]
        };
    }
}