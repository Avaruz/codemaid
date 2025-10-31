using Microsoft.VisualStudio.TestTools.UnitTesting;
using ASGV.CodeMaid.Properties;
using System;

namespace ASGV.CodeMaid.UnitTests.Formatting
{
    /// <summary>
    /// </summary>
    [TestClass]
    public class FormatWithPrefixTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Settings.Default.Reset();
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void SimpleFormatWithPrefixTests_KeepsPrefix()
        {
      string input = "// Lorem ipsum dolor sit amet, consectetur adipiscing elit.";
      string expected =
                "// Lorem ipsum dolor sit amet," + Environment.NewLine +
                "// consectetur adipiscing elit.";
            CommentFormatHelper.AssertEqualAfterFormat(input, expected, "//", o => o.WrapColumn = 40);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void SimpleFormatWithPrefixTests_TrimsTrailingSpace()
        {
      string input = "// Trailing space  ";
      string expected = "// Trailing space";
            CommentFormatHelper.AssertEqualAfterFormat(input, expected, "//");
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void SimpleFormatWithPrefixTests_TrimsTrailingLines()
        {
      string input =
                "// Comment with some trailing lines" + Environment.NewLine +
                "//" + Environment.NewLine +
                "//";
      string expected =
                "// Comment with some trailing lines";
            CommentFormatHelper.AssertEqualAfterFormat(input, expected, "//");
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void SimpleFormatWithPrefixTests_TrimsLeadingLines()
        {
      string input =
                "//" + Environment.NewLine +
                "//" + Environment.NewLine +
                "// Comment with some leading lines";
      string expected =
                "// Comment with some leading lines";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, "//");
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void SimpleFormatWithPrefixTests_KeepsLeadingSpace()
        {
      string input = "    // Lorem ipsum.";
            CommentFormatHelper.AssertEqualAfterFormat(input, input, "    //");
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void SimpleFormatWithPrefixTests_AlignsToFirstPrefix()
        {
      string input =
                "    // Lorem ipsum dolor sit amet, consectetur" + Environment.NewLine +
                "  // adipiscing elit.";
      string expected =
                "    // Lorem ipsum dolor sit amet," + Environment.NewLine +
                "    // consectetur adipiscing elit.";
            CommentFormatHelper.AssertEqualAfterFormat(input, expected, "    //", o => o.WrapColumn = 40);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void SimpleFormatWithPrefixTests_NoTrailingWhitespaceOnEmptyLine()
        {
      string input =
                "// Lorem ipsum dolor sit amet." + Environment.NewLine +
                "//" + Environment.NewLine +
                "// Consectetur adipiscing elit.";
            CommentFormatHelper.AssertEqualAfterFormat(input, input, "//", o => o.WrapColumn = 40);
        }
    }
}