namespace Gossamer.Tests.Gossamer;

[TestClass]
public class IdentityTest
{
    [TestMethod]
    public void Identity_Create()
    {
        string name = "TestName";
        Identity identity = Identity.Create(name);
        Assert.AreEqual(name, identity.Name);
        Assert.AreNotEqual(Guid.Empty, identity.Id);

        identity = Identity.Create();
        Assert.AreEqual(identity.Id.ToString(), identity.Name);
        Assert.AreNotEqual(Guid.Empty, identity.Id);
    }

    [TestMethod]
    public void Identity_ToString()
    {
        string name = "TestName";
        Identity identity = Identity.Create(name);
        string result = identity.ToString();
        Assert.AreEqual(name, result);

        identity = Identity.Create();
        Assert.AreEqual(identity.Id.ToString(), identity.ToString());
    }

    [TestMethod]
    public void Identity_Equals()
    {
        Identity identity1 = Identity.Create();
        Identity identity2 = new(identity1.Id, "TestName");
        bool result = identity1.Equals(identity2);
        Assert.IsTrue(result);

        identity1 = Identity.Create();
        identity2 = Identity.Create();
        result = identity1.Equals(identity2);
        Assert.IsFalse(result);

        Identity identity = Identity.Create();
        result = identity.Equals(null);
        Assert.IsFalse(result);

        identity = Identity.Create();
        result = identity.Equals(new object());
        Assert.IsFalse(result);

        identity = Identity.Create();
        result = identity.Equals(identity);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Identity_GetHashCode()
    {
        Identity identity1 = Identity.Create();
        Identity identity2 = new(identity1.Id, "TestName");
        int hashCode1 = identity1.GetHashCode();
        int hashCode2 = identity2.GetHashCode();
        Assert.AreEqual(hashCode1, hashCode2);

        identity1 = Identity.Create();
        identity2 = Identity.Create();
        hashCode1 = identity1.GetHashCode();
        hashCode2 = identity2.GetHashCode();
        Assert.AreNotEqual(hashCode1, hashCode2);
    }
}