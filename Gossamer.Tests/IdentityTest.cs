namespace Gossamer.Tests;

[TestClass]
public class IdentityTest
{
    [TestMethod]
    public void Create_WithName_ShouldReturnIdentityWithName()
    {
        // Arrange
        string name = "TestName";

        // Act
        Identity identity = Identity.Create(name);

        // Assert
        Assert.AreEqual(name, identity.Name);
        Assert.AreNotEqual(Guid.Empty, identity.Id);
    }

    [TestMethod]
    public void Create_WithoutName_ShouldReturnIdentityWithIdAsName()
    {
        // Act
        Identity identity = Identity.Create();

        // Assert
        Assert.AreEqual(identity.Id.ToString(), identity.Name);
        Assert.AreNotEqual(Guid.Empty, identity.Id);
    }

    [TestMethod]
    public void ToString_WithName_ShouldReturnName()
    {
        // Arrange
        string name = "TestName";
        Identity identity = Identity.Create(name);

        // Act
        string result = identity.ToString();

        // Assert
        Assert.AreEqual(name, result);
    }

    [TestMethod]
    public void ToString_WithoutName_ShouldReturnId()
    {
        // Act
        Identity identity = Identity.Create();

        // Assert
        Assert.AreEqual(identity.Id.ToString(), identity.ToString());
    }

    [TestMethod]
    public void Equals_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        Identity identity1 = Identity.Create();
        Identity identity2 = new Identity(identity1.Id, "TestName");

        // Act
        bool result = identity1.Equals(identity2);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Equals_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        Identity identity1 = Identity.Create();
        Identity identity2 = Identity.Create();

        // Act
        bool result = identity1.Equals(identity2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void GetHashCode_WithSameId_ShouldReturnSameHashCode()
    {
        // Arrange
        Identity identity1 = Identity.Create();
        Identity identity2 = new Identity(identity1.Id, "TestName");

        // Act
        int hashCode1 = identity1.GetHashCode();
        int hashCode2 = identity2.GetHashCode();

        // Assert
        Assert.AreEqual(hashCode1, hashCode2);
    }

    [TestMethod]
    public void GetHashCode_WithDifferentId_ShouldReturnDifferentHashCode()
    {
        // Arrange
        Identity identity1 = Identity.Create();
        Identity identity2 = Identity.Create();

        // Act
        int hashCode1 = identity1.GetHashCode();
        int hashCode2 = identity2.GetHashCode();

        // Assert
        Assert.AreNotEqual(hashCode1, hashCode2);
    }

    [TestMethod]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        Identity identity = Identity.Create();

        // Act
        bool result = identity.Equals(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        Identity identity = Identity.Create();

        // Act
        bool result = identity.Equals(new object());

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Equals_WithSameIdentity_ShouldReturnTrue()
    {
        // Arrange
        Identity identity = Identity.Create();

        // Act
        bool result = identity.Equals(identity);

        // Assert
        Assert.IsTrue(result);
    }
}