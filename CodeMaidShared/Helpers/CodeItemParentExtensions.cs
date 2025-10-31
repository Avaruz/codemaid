using ASGV.CodeMaid.Model.CodeItems;

namespace ASGV.CodeMaid.Helpers
{
    /// <summary>
    /// A set of extension methods for <see cref="ICodeItemParent" />.
    /// </summary>
    public static class CodeItemParentExtensions
    {
        /// <summary>
        /// Recursively gets the children in a depth-first fashion for the specified parent without
        /// delving into nested element parents.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The recursive set of children.</returns>
        public static SetCodeItems GetChildrenRecursive(this ICodeItemParent parent)
        {
      SetCodeItems children = new();

            foreach (BaseCodeItem child in parent.Children)
            {
                children.Add(child);

        ICodeItemParent childAsParent = child as ICodeItemParent;
                if (childAsParent != null && child is not BaseCodeItemElementParent)
                {
                    children.AddRange(childAsParent.GetChildrenRecursive());
                }
            }

            return children;
        }
    }
}