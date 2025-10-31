using Microsoft.VisualStudio.TestTools.UnitTesting;
using ASGV.CodeMaid.Helpers;

namespace ASGV.CodeMaid.UnitTests
{
    [TestClass]
    public class MemberTypeSettingTests
    {
        [TestMethod]
        public void CanSerializeMemberTypeSetting()
        {
      MemberTypeSetting memberTypeSetting = new("Fields", "Member Variables", 1);
            Assert.IsNotNull(memberTypeSetting);

      string serializedString = (string)memberTypeSetting;
            Assert.IsFalse(string.IsNullOrWhiteSpace(serializedString));
        }

        [TestMethod]
        public void CanDeserializeMemberTypeSetting()
        {
            const string serializedString = "Fields||1||Member Variables";

      MemberTypeSetting memberTypeSetting = (MemberTypeSetting)serializedString;

            Assert.IsNotNull(memberTypeSetting);
            Assert.AreEqual(memberTypeSetting.DefaultName, "Fields");
            Assert.AreEqual(memberTypeSetting.EffectiveName, "Member Variables");
            Assert.AreEqual(memberTypeSetting.Order, 1);
        }
    }
}