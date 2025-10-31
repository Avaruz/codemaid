using Microsoft.VisualStudio.TestTools.UnitTesting;
using ASGV.CodeMaid.Model.Comments.Options;
using ASGV.CodeMaid.Properties;
using System;

namespace ASGV.CodeMaid.UnitTests.Formatting
{
    /// <summary>
    /// Class with simple unit tests for formatting XML based comments. This calls the formatter
    /// directly, rather than invoking it through the UI as with the integration tests.
    /// </summary>
    [TestClass]
    public class XmlFormattingTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Settings.Default.Reset();
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_AddSpaceToInsideTags()
        {
      string input = "<xml><see/></xml>";
      string expected = "<xml><see /></xml>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o =>
            {
                o.Xml.Default.SpaceSelfClosing = true;
            });
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_AddSpaceToTagContent()
        {
      string input = "<xml><c>test</c></xml>";
      string expected = "<xml> <c> test </c> </xml>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o =>
            {
                o.Xml.Default.SpaceContent = true;
            });
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_AddSpaceToTagContentWithSelfClosingTag()
        {
      string input = "<tag1><tag2/></tag1>";
      string expected = "<tag1> <tag2/> </tag1>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o =>
            {
                o.Xml.Default.SpaceContent = true;
                o.Xml.Default.SpaceSelfClosing = false;
            });
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_AddSpaceToTagContentWithSelfClosingTagMultiline()
        {
      // Add space to content should not add a space when tag content is on it's own line.
      string input = "<tag1><tag2/></tag1>";
      string expected =
                "<tag1>" + Environment.NewLine +
                "<tag2/>" + Environment.NewLine +
                "</tag1>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o =>
            {
                o.Xml.Default.Split = XmlTagNewLine.Always;
                o.Xml.Default.SpaceContent = true;
                o.Xml.Default.SpaceSelfClosing = false;
            });
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_AddSpaceToTagContentShouldLeaveNoTrailingWhitespace1()
        {
      string input = "<xml>Lorem ipsum dolor sit amet, consectetur adipiscing elit.</xml>";
      string expected =
                "<xml>" + Environment.NewLine +
                "Lorem ipsum dolor sit amet," + Environment.NewLine +
                "consectetur adipiscing elit." + Environment.NewLine +
                "</xml>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o =>
            {
                o.WrapColumn = 30;
                o.Xml.Default.Split = XmlTagNewLine.Always;
                o.Xml.Default.SpaceContent = true;
            });
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_AddSpaceToTagContentShouldLeaveNoTrailingWhitespace2()
        {
      string input =
               "<remarks>" + Environment.NewLine +
               "Lorem ipsum dolor sit amet, consectetur adipiscing elit." + Environment.NewLine +
               "</remarks>";

      string expected =
               "<remarks>" + Environment.NewLine +
               "    Lorem ipsum dolor sit amet, consectetur" + Environment.NewLine +
               "    adipiscing elit." + Environment.NewLine +
               "</remarks>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o =>
            {
                o.WrapColumn = 50;
                o.Xml.Default.Indent = 4;
                o.Xml.Default.SpaceContent = true;
            });
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_AllRootLevelTagsOnNewLine()
        {
      string input = "<tag1>abc</tag1><tag2>abc</tag2>";
      string expected =
                "<tag1>abc</tag1>" + Environment.NewLine +
                "<tag2>abc</tag2>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_BreakAllTags()
        {
      string input = "<tag1></tag1><tag2></tag2>";
      string expected =
                "<tag1>" + Environment.NewLine +
                "</tag1>" + Environment.NewLine +
                "<tag2>" + Environment.NewLine +
                "</tag2>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o =>
            {
                o.Xml.Default.Indent = 0;
                o.Xml.Default.Split = XmlTagNewLine.Always;
            });
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_BreakLongParagraphs()
        {
      string input = "<example><para>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus nisi neque, placerat sed neque vitae.</para></example>";
      string expected =
                "<example>" + Environment.NewLine +
                "<para>" + Environment.NewLine +
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit." + Environment.NewLine +
                "Vivamus nisi neque, placerat sed neque vitae." + Environment.NewLine +
                "</para>" + Environment.NewLine +
                "</example>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o => o.WrapColumn = 60);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_BreakTagsWhenContainsParagraphs()
        {
      string input = "<example><para>test</para></example>";
      string expected =
                "<example>" + Environment.NewLine +
                "<para>test</para>" + Environment.NewLine +
                "</example>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected);
        }

        /// <summary>
        /// If XML tag indenting is set, this should not affect any literal content. However, content
        /// after the literal should be indented as normal.
        /// </summary>
        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_DoesIndentAfterLiteralContent()
        {
      string input =
               "<example>" + Environment.NewLine +
               "Example usage :" + Environment.NewLine +
               "<code source=\"..\\MyExamples\\Examples.cs\" region=\"Example1\" language=\"cs\"/>" + Environment.NewLine +
               "Example usage with a location parameter and a location function:" + Environment.NewLine +
               "<code source=\"..\\MyExamples\\Examples.cs\" region=\"Example2\" language=\"cs\"/>" + Environment.NewLine +
               "And some final text that should also be formatted." + Environment.NewLine +
               "</example>";

      string expected =
               "<example>" + Environment.NewLine +
               "    Example usage :" + Environment.NewLine +
               "    <code source=\"..\\MyExamples\\Examples.cs\" region=\"Example1\" language=\"cs\"/>" + Environment.NewLine +
               "    Example usage with a location parameter and a location function:" + Environment.NewLine +
               "    <code source=\"..\\MyExamples\\Examples.cs\" region=\"Example2\" language=\"cs\"/>" + Environment.NewLine +
               "    And some final text that should also be formatted." + Environment.NewLine +
               "</example>";

            Settings.Default.Formatting_CommentXmlValueIndent = 4;
            Settings.Default.Formatting_CommentXmlKeepTagsTogether = true;
            Settings.Default.Formatting_CommentXmlSpaceSingleTags = false;

      // First pass.
      string result = CommentFormatHelper.AssertEqualAfterFormat(input, expected);

            // Second pass.
            CommentFormatHelper.AssertEqualAfterFormat(result, expected);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_DoesNotIndentCloseTag()
        {
      string input = "<tag1></tag1><tag2></tag2>";
      string expected =
                "<tag1>" + Environment.NewLine +
                "</tag1>" + Environment.NewLine +
                "<tag2></tag2>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o =>
            {
                o.Xml.Default.Indent = 4;
                o.Xml.Default.KeepTogether = true;

                o.Xml.Tags.Clear();
                o.Xml.Tags["tag1"] = new FormatterOptionsXmlTag { Split = XmlTagNewLine.Always };
            });
        }

        /// <summary>
        /// If XML tag indenting is set, this should not affect any literal content. Since whitespace
        /// is preserved on literals, this would increase the indenting with every pass.
        /// </summary>
        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_DoesNotIndentLiteralContent()
        {
      string input =
               "<test>" + Environment.NewLine +
               "<code>" + Environment.NewLine +
               "    Some code with." + Environment.NewLine +
               "   funny indenting" + Environment.NewLine +
               "" + Environment.NewLine +
               "  and a white line" + Environment.NewLine +
               "that should not change." + Environment.NewLine +
               "</code>" + Environment.NewLine +
               "</test>";

      string expected =
               "<test>" + Environment.NewLine +
               "    <code>" + Environment.NewLine +
               "    Some code with." + Environment.NewLine +
               "   funny indenting" + Environment.NewLine +
               "" + Environment.NewLine +
               "  and a white line" + Environment.NewLine +
               "that should not change." + Environment.NewLine +
               "    </code>" + Environment.NewLine +
               "</test>";

            Settings.Default.Formatting_CommentXmlValueIndent = 4;

      // First pass.
      string result = CommentFormatHelper.AssertEqualAfterFormat(input, expected);

            // Second pass.
            CommentFormatHelper.AssertEqualAfterFormat(result, expected);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_DoNotAutoCollapseTags()
        {
            CommentFormatHelper.AssertEqualAfterFormat("<xml></xml>");
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_DoNotAutoExpandTags()
        {
            CommentFormatHelper.AssertEqualAfterFormat("<xml/>", o => o.Xml.Default.SpaceSelfClosing = false);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_HyperlinkBetweenWords()
        {
      string input = "<summary>" + Environment.NewLine + "Look at this http://foo pretty link." + Environment.NewLine + "</summary>";
            CommentFormatHelper.AssertEqualAfterFormat(input);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_HyperlinkOnNewLine()
        {
      string input = "<summary>" + Environment.NewLine + "http://foo" + Environment.NewLine + "</summary>";
            CommentFormatHelper.AssertEqualAfterFormat(input);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_IndentsXml()
        {
      string input = "<summary>Lorem ipsum dolor sit amet.</summary>";
      string expected =
                "<summary>" + Environment.NewLine +
                "    Lorem ipsum dolor sit amet." + Environment.NewLine +
                "</summary>";

            Settings.Default.Formatting_CommentXmlSplitSummaryTagToMultipleLines = true;
            Settings.Default.Formatting_CommentXmlValueIndent = 4;

            CommentFormatHelper.AssertEqualAfterFormat(input, expected);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_IndentsXmlMultiLevel()
        {
      string input = "<summary>Lorem ipsum dolor <para>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus nisi neque, placerat sed neque vitae.</para> sit amet.</summary>";
      string expected =
                "<summary>" + Environment.NewLine +
                "    Lorem ipsum dolor" + Environment.NewLine +
                "    <para>" + Environment.NewLine +
                "        Lorem ipsum dolor sit amet, consectetur adipiscing" + Environment.NewLine +
                "        elit. Vivamus nisi neque, placerat sed neque vitae." + Environment.NewLine +
                "    </para>" + Environment.NewLine +
                "    sit amet." + Environment.NewLine +
                "</summary>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o =>
            {
                o.WrapColumn = 60;
                o.Xml.Default.Indent = 4;
            });
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_IndentsXmlSingleLevel()
        {
      string input = "<summary>Lorem ipsum dolor <para>Lorem ipsum dolor sit amet.</para> sit amet.</summary>";
      string expected =
                "<summary>" + Environment.NewLine +
                "    Lorem ipsum dolor" + Environment.NewLine +
                "    <para>Lorem ipsum dolor sit amet.</para>" + Environment.NewLine +
                "    sit amet." + Environment.NewLine +
                "</summary>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o =>
            {
                o.Xml.Default.Indent = 4;
                o.Xml.Tags["summary"] = new FormatterOptionsXmlTag { Split = XmlTagNewLine.Always };
            });
        }

        /// <summary>
        /// Test to make sure there is no spacing is added between an inline XML tag directly
        /// followed by interpunction.
        /// </summary>
        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_InterpunctionNoSpacing()
        {
      string input = "<test>Line with <interpunction/>.</test>";

            CommentFormatHelper.AssertEqualAfterFormat(input, o => o.Xml.Default.SpaceSelfClosing = false);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_KeepShortParagraphs()
        {
      string input =
                "<test>" + Environment.NewLine +
                "<para>" + Environment.NewLine +
                "Lorem ipsum dolor sit amet." + Environment.NewLine +
                "</para>" + Environment.NewLine +
                "</test>";

      string expected =
                "<test>" + Environment.NewLine +
                "<para>Lorem ipsum dolor sit amet.</para>" + Environment.NewLine +
                "</test>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_RemoveSpaceFromInsideTags()
        {
      string input = "<xml><see /></xml>";
      string expected = "<xml><see/></xml>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o => o.Xml.Default.SpaceSelfClosing = false);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_RemoveSpaceFromTagContent()
        {
      string input = "<xml> <c> test </c> </xml>";
      string expected = "<xml><c>test</c></xml>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o => o.Xml.Default.SpaceContent = false);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_SplitAlwaysOnSingleTag()
        {
      string input = "<tag1></tag1><tag2></tag2>";
      string expected =
                "<tag1>" + Environment.NewLine +
                "</tag1>" + Environment.NewLine +
                "<tag2></tag2>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o =>
            {
                o.Xml.Default.Indent = 0;
                o.Xml.Tags.Clear();
                o.Xml.Tags["tag1"] = new FormatterOptionsXmlTag { Split = XmlTagNewLine.Always };
            });
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_SplitsTagsWhenLineDoesNotFit()
        {
      string input = "<test>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vivamus nisi neque, placerat sed neque vitae</test>";
      string expected = "<test>" + Environment.NewLine +
                "Lorem ipsum dolor sit amet, consectetur adipiscing" + Environment.NewLine +
                "elit. Vivamus nisi neque, placerat sed neque vitae" + Environment.NewLine +
                "</test>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o =>
            {
                o.WrapColumn = 50;
                o.SkipWrapOnLastWord = false;
            });
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_TagCase_Keep()
        {
      string input = "<Xml></Xml>";

            CommentFormatHelper.AssertEqualAfterFormat(input, o => o.Xml.Default.Case = XmlTagCase.Keep);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_TagCase_Lower()
        {
      string input = "<Xml></Xml>";
      string expected = "<xml></xml>";
            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o => o.Xml.Default.Case = XmlTagCase.LowerCase);
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_TagCase_Upper()
        {
      string input = "<Xml></Xml>";
      string expected = "<XML></XML>";
            CommentFormatHelper.AssertEqualAfterFormat(input, expected, o => o.Xml.Default.Case = XmlTagCase.UpperCase);
        }

        /// <summary>
        /// If XML tag indenting is set, this should not affect any literal content. Since whitespace
        /// is preserved on literals, this would increase the indenting with every pass.
        /// </summary>
        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_Literal_DoesNotIndent()
        {
      string input =
               "<test>" + Environment.NewLine +
               "<code>" + Environment.NewLine +
               "    Some code with." + Environment.NewLine +
               "   funny indenting" + Environment.NewLine +
               "" + Environment.NewLine +
               "  and a white line" + Environment.NewLine +
               "that should not change." + Environment.NewLine +
               "</code>" + Environment.NewLine +
               "</test>";

      string expected =
               "<test>" + Environment.NewLine +
               "    <code>" + Environment.NewLine +
               "    Some code with." + Environment.NewLine +
               "   funny indenting" + Environment.NewLine +
               "" + Environment.NewLine +
               "  and a white line" + Environment.NewLine +
               "that should not change." + Environment.NewLine +
               "    </code>" + Environment.NewLine +
               "</test>";

      // First pass.
      string result = CommentFormatHelper.AssertEqualAfterFormat(input, expected, o => o.Xml.Default.Indent = 4);

            // Second pass.
            CommentFormatHelper.AssertEqualAfterFormat(result, expected, o => o.Xml.Default.Indent = 4);
        }

        /// <summary>
        /// If XML tag indenting is set, this should not affect any literal content. however, content
        /// after the literal should be indented as normal.
        /// </summary>
        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_Literal_IndentsAfterContent()
        {
      string input =
               "<example>" + Environment.NewLine +
               "Example usage :" + Environment.NewLine +
               "<code source=\"..\\MyExamples\\Examples.cs\" region=\"Example1\" language=\"cs\"/>" + Environment.NewLine +
               "Example usage with a location parameter and a location function:" + Environment.NewLine +
               "<code source=\"..\\MyExamples\\Examples.cs\" region=\"Example2\" language=\"cs\"/>" + Environment.NewLine +
               "And some final text that should also be formatted." + Environment.NewLine +
               "</example>";

      string expected =
               "<example>" + Environment.NewLine +
               "    Example usage :" + Environment.NewLine +
               "    <code source=\"..\\MyExamples\\Examples.cs\" region=\"Example1\" language=\"cs\"/>" + Environment.NewLine +
               "    Example usage with a location parameter and a location function:" + Environment.NewLine +
               "    <code source=\"..\\MyExamples\\Examples.cs\" region=\"Example2\" language=\"cs\"/>" + Environment.NewLine +
               "    And some final text that should also be formatted." + Environment.NewLine +
               "</example>";

      // First pass.
      string result = CommentFormatHelper.AssertEqualAfterFormat(input, expected, o =>
            {
                o.Xml.Default.Indent = 4;
                o.Xml.Default.KeepTogether = true;
                o.Xml.Default.SpaceSelfClosing = false;
            });

            // Second pass.
            CommentFormatHelper.AssertEqualAfterFormat(result, expected, o =>
            {
                o.Xml.Default.Indent = 4;
                o.Xml.Default.KeepTogether = true;
                o.Xml.Default.SpaceSelfClosing = false;
            });
        }

        [TestMethod]
        [TestCategory("Formatting UnitTests")]
        public void XmlFormattingTests_Literal_KeepFormatting()
        {
      string input =
                "<test>before <code>" + Environment.NewLine +
                "some" + Environment.NewLine +
                "  code" + Environment.NewLine +
                "stuff" + Environment.NewLine +
                "</code> after</test>";

      string expected =
                "<test>" + Environment.NewLine +
                "before" + Environment.NewLine +
                "<code>" + Environment.NewLine +
                "some" + Environment.NewLine +
                "  code" + Environment.NewLine +
                "stuff" + Environment.NewLine +
                "</code>" + Environment.NewLine +
                "after" + Environment.NewLine +
                "</test>";

            CommentFormatHelper.AssertEqualAfterFormat(input, expected);
        }
    }
}