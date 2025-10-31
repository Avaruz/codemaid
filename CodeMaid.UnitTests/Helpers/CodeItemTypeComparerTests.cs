using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ASGV.CodeMaid.Helpers;
using ASGV.CodeMaid.Model.CodeItems;
using ASGV.CodeMaid.Properties;

namespace ASGV.CodeMaid.UnitTests.Helpers
{
  [TestClass]
  public class CodeItemTypeComparerTests
  {
    [TestInitialize]
    public void TestInitialize()
    {
      Settings.Default.Reset();
    }

    [TestMethod]
    public void ShouldSortItemsOfTheSameTypeByName()
    {
      BaseCodeItem itemB = Create<CodeItemField>("b", 1);
      BaseCodeItem itemA = Create<CodeItemField>("a", 2);
      CodeItemTypeComparer comparer = new(sortByName: true);

      int result = comparer.Compare(itemA, itemB);

      Assert.IsTrue(result < 0);
    }

    [TestMethod]
    public void ShouldSortItemsOfTheSameTypeByOffset()
    {
      BaseCodeItem itemB = Create<CodeItemField>("b", 1);
      BaseCodeItem itemA = Create<CodeItemField>("a", 2);
      CodeItemTypeComparer comparer = new(sortByName: false);

      int result = comparer.Compare(itemA, itemB);

      Assert.IsTrue(result > 0);
    }

    [TestMethod]
    public void ShouldSortByGroupType()
    {
      BaseCodeItem method = Create<CodeItemMethod>("a", 1);
      BaseCodeItem field = Create<CodeItemField>("z", 2);
      CodeItemTypeComparer comparer = new(sortByName: true);

      int result = comparer.Compare(field, method);

      Assert.IsTrue(result < 0);
    }

    [TestMethod]
    public void ShouldSortByExplicitInterfaceMemberName()
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      CodeItemMethod methodZ = CreateExplicitMethod("Interface", "Z", 1);
      BaseCodeItem methodX = Create<CodeItemMethod>("X", 2);
      CodeItemTypeComparer comparer = new(sortByName: true);

      Settings.Default.Reorganizing_ExplicitMembersAtEnd = false;
      int result = comparer.Compare(methodX, methodZ);

      Assert.IsTrue(result < 0);
    }

    [TestMethod]
    public void ShouldPlaceExplicitInterfaceMembersAtTheEndOfTheGroup()
    {
      CodeItemMethod methodA = CreateExplicitMethod("Interface", "A", 1);
      BaseCodeItem methodB = Create<CodeItemMethod>("B", 2);
      CodeItemTypeComparer comparer = new(sortByName: true);

      Settings.Default.Reorganizing_ExplicitMembersAtEnd = true;
      int result = comparer.Compare(methodB, methodA);

      Assert.IsTrue(result < 0);
    }

    private static T Create<T>(string name, int offset) where T : BaseCodeItem, new()
    {
      return new T
      {
        Name = name,
        StartOffset = offset
      };
    }

    private static CodeItemMethod CreateExplicitMethod(string interfaceName, string methodName, int offset)
    {
      CodeItemMethod method = Create<CodeItemMethod>(interfaceName + "." + methodName, offset);
      method.CodeFunction = Substitute.For<CodeFunction2>();
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
      method.CodeFunction.Name = method.Name;
      return method;
    }
  }
}