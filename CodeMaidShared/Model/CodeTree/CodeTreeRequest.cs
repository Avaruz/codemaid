using EnvDTE;
using ASGV.CodeMaid.Model.CodeItems;

namespace ASGV.CodeMaid.Model.CodeTree
{
    /// <summary>
    /// A simple class for containing a request to build a code tree.
    /// </summary>
    internal class CodeTreeRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeTreeRequest" /> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="rawCodeItems">The raw code items.</param>
        /// <param name="sortOrder">The sort order.</param>
        /// <param name="nameFilter">The name filter.</param>
        internal CodeTreeRequest(Document document, SetCodeItems rawCodeItems, CodeSortOrder sortOrder, string nameFilter = null)
        {
            Document = document;
            RawCodeItems = rawCodeItems;
            SortOrder = sortOrder;
            NameFilter = nameFilter;
        }

        /// <summary>
        /// Gets the document.
        /// </summary>
        internal Document Document { get; }

        /// <summary>
        /// Gets the raw code items.
        /// </summary>
        internal SetCodeItems RawCodeItems { get; }

        /// <summary>
        /// Gets the sort order.
        /// </summary>
        internal CodeSortOrder SortOrder { get; }

        /// <summary>
        /// Gets the name filter.
        /// </summary>
        internal string NameFilter { get; }
    }
}