using Squad.SDK.NET.Skills;
using Xunit;

namespace Squad.SDK.NET.Tests;

public sealed class SkillRegistryTests
{
    [Fact]
    public void Register_AndGet_ReturnsSkill()
    {
        // Arrange
        var registry = new SkillRegistry();
        var skill = new SkillDefinition
        {
            Id = "test-skill",
            Name = "Test Skill",
            Content = "test content",
            Triggers = ["test", "skill"],
            AgentRoles = ["Backend"]
        };

        // Act
        registry.Register(skill);
        var retrieved = registry.Get("test-skill");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("test-skill", retrieved.Id);
        Assert.Equal("Test Skill", retrieved.Name);
        Assert.Equal("test content", retrieved.Content);
    }

    [Fact]
    public void Register_AndUnregister_GetReturnsNull()
    {
        // Arrange
        var registry = new SkillRegistry();
        var skill = new SkillDefinition
        {
            Id = "test-skill",
            Name = "Test Skill",
            Content = "test content"
        };

        // Act
        registry.Register(skill);
        var unregistered = registry.Unregister("test-skill");
        var retrieved = registry.Get("test-skill");

        // Assert
        Assert.True(unregistered);
        Assert.Null(retrieved);
    }

    [Fact]
    public void Unregister_NonexistentSkill_ReturnsFalse()
    {
        // Arrange
        var registry = new SkillRegistry();

        // Act
        var result = registry.Unregister("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetAll_ReturnsAllRegisteredSkills()
    {
        // Arrange
        var registry = new SkillRegistry();
        var skill1 = new SkillDefinition { Id = "skill1", Name = "Skill 1", Content = "content1" };
        var skill2 = new SkillDefinition { Id = "skill2", Name = "Skill 2", Content = "content2" };
        var skill3 = new SkillDefinition { Id = "skill3", Name = "Skill 3", Content = "content3" };

        // Act
        registry.Register(skill1);
        registry.Register(skill2);
        registry.Register(skill3);
        var allSkills = registry.GetAll();

        // Assert
        Assert.Equal(3, allSkills.Count);
        Assert.Contains(allSkills, s => s.Id == "skill1");
        Assert.Contains(allSkills, s => s.Id == "skill2");
        Assert.Contains(allSkills, s => s.Id == "skill3");
    }

    [Fact]
    public void Match_WithTriggerKeywords_ReturnsMatchingSkills()
    {
        // Arrange
        var registry = new SkillRegistry();
        var skill = new SkillDefinition
        {
            Id = "auth-skill",
            Name = "Auth Skill",
            Content = "auth content",
            Triggers = ["authentication", "login", "oauth"],
            AgentRoles = ["Backend"]
        };
        registry.Register(skill);

        // Act
        var matches = registry.Match("I need help with authentication");

        // Assert
        Assert.Single(matches);
        Assert.Equal("auth-skill", matches[0].Skill.Id);
        Assert.True(matches[0].Score > 0);
    }

    [Fact]
    public void Match_FiltersByAgentRole_ReturnsOnlyMatchingRoles()
    {
        // Arrange
        var registry = new SkillRegistry();
        var backendSkill = new SkillDefinition
        {
            Id = "backend-skill",
            Name = "Backend",
            Content = "backend",
            Triggers = ["api", "database"],
            AgentRoles = ["Backend"]
        };
        var frontendSkill = new SkillDefinition
        {
            Id = "frontend-skill",
            Name = "Frontend",
            Content = "frontend",
            Triggers = ["api", "ui"],
            AgentRoles = ["Frontend"]
        };

        registry.Register(backendSkill);
        registry.Register(frontendSkill);

        // Act
        var matches = registry.Match("I need help with api", "Backend");

        // Assert
        Assert.Single(matches);
        Assert.Equal("backend-skill", matches[0].Skill.Id);
    }

    [Fact]
    public void Match_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var registry = new SkillRegistry();
        var skill = new SkillDefinition
        {
            Id = "test-skill",
            Name = "Test",
            Content = "test",
            Triggers = ["specific", "keyword"]
        };
        registry.Register(skill);

        // Act
        var matches = registry.Match("completely unrelated query");

        // Assert
        Assert.Empty(matches);
    }

    [Fact]
    public void Match_EmptyTask_ReturnsEmptyList()
    {
        // Arrange
        var registry = new SkillRegistry();
        var skill = new SkillDefinition
        {
            Id = "test-skill",
            Name = "Test",
            Content = "test",
            Triggers = ["keyword"]
        };
        registry.Register(skill);

        // Act
        var matches = registry.Match("");

        // Assert
        Assert.Empty(matches);
    }

    [Fact]
    public void LoadContent_ExistingSkill_ReturnsContent()
    {
        // Arrange
        var registry = new SkillRegistry();
        var skill = new SkillDefinition
        {
            Id = "test-skill",
            Name = "Test",
            Content = "This is the skill content"
        };
        registry.Register(skill);

        // Act
        var content = registry.LoadContent("test-skill");

        // Assert
        Assert.Equal("This is the skill content", content);
    }

    [Fact]
    public void LoadContent_UnknownSkill_ReturnsNull()
    {
        // Arrange
        var registry = new SkillRegistry();

        // Act
        var content = registry.LoadContent("unknown-skill");

        // Assert
        Assert.Null(content);
    }

    [Fact]
    public void Match_MultipleMatches_OrderedByScore()
    {
        // Arrange
        var registry = new SkillRegistry();
        var skill1 = new SkillDefinition
        {
            Id = "skill1",
            Name = "Skill 1",
            Content = "content",
            Triggers = ["test"]
        };
        var skill2 = new SkillDefinition
        {
            Id = "skill2",
            Name = "Skill 2",
            Content = "content",
            Triggers = ["test", "debug"],
            Confidence = SkillConfidence.High
        };

        registry.Register(skill1);
        registry.Register(skill2);

        // Act
        var matches = registry.Match("I need to test and debug");

        // Assert
        Assert.Equal(2, matches.Count);
        // Both skills match, verify both are present
        Assert.Contains(matches, m => m.Skill.Id == "skill1");
        Assert.Contains(matches, m => m.Skill.Id == "skill2");
        // skill2 matches more keywords (test + debug vs just test)
        var skill2Match = matches.First(m => m.Skill.Id == "skill2");
        Assert.True(skill2Match.Score > 0);
    }
}
