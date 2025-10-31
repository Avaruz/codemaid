using EnvDTE;
using ASGV.CodeMaid.Model.CodeItems;
using ASGV.CodeMaid.Properties;
using System.Collections.Generic;

namespace ASGV.CodeMaid.Helpers
{
    /// <summary>
    /// A helper for comparing code items by type, access level, etc.
    /// </summary>
    public class CodeItemTypeComparer : Comparer<BaseCodeItem>
    {
        #region Fields

        private readonly bool _sortByName;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeItemTypeComparer"/> class.
        /// </summary>
        /// <param name="sortByName">Determines whether a secondary sort by name is performed or not.</param>
        public CodeItemTypeComparer(bool sortByName)
        {
            _sortByName = sortByName;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Performs a comparison of two objects of the same type and returns a value indicating
        /// whether one object is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>
        /// Less than zero: <paramref name="x" /> is less than <paramref name="y" />.
        /// Zero: <paramref name="x" /> equals <paramref name="y" />.
        /// Greater than zero: <paramref name="x" /> is greater than <paramref name="y" />.
        /// </returns>
        public override int Compare(BaseCodeItem x, BaseCodeItem y)
        {
            int first = CalculateNumericRepresentation(x);
            int second = CalculateNumericRepresentation(y);

            if (first == second)
            {
                // Check if secondary sort by name should occur.
                if (_sortByName)
                {
                    int nameComparison = NormalizeName(x).CompareTo(NormalizeName(y));
                    if (nameComparison != 0)
                    {
                        return nameComparison;
                    }
                }

                // Fall back to position comparison for matching elements.
                return x.StartOffset.CompareTo(y.StartOffset);
            }

            return first.CompareTo(second);
        }

        private static int CalculateNumericRepresentation(BaseCodeItem codeItem)
        {
            int typeOffset = CalculateTypeOffset(codeItem);
            int accessOffset = CalculateAccessOffset(codeItem);
            int explicitOffset = CalculateExplicitInterfaceOffset(codeItem);
            int constantOffset = CalculateConstantOffset(codeItem);
            int staticOffset = CalculateStaticOffset(codeItem);
            int readOnlyOffset = CalculateReadOnlyOffset(codeItem);

            int calc = 0;

            if (!Settings.Default.Reorganizing_PrimaryOrderByAccessLevel)
            {
                calc += typeOffset * 100000;
                calc += accessOffset * 10000;
            }
            else
            {
                calc += accessOffset * 100000;
                calc += typeOffset * 10000;
            }

            calc += (explicitOffset * 1000) + (constantOffset * 100) + (staticOffset * 10) + readOnlyOffset;

            return calc;
        }

        private static int CalculateTypeOffset(BaseCodeItem codeItem)
        {
      return codeItem.Kind switch
      {
        KindCodeItem.Class => MemberTypeSettingHelper.ClassSettings.Order,
        KindCodeItem.Constructor => MemberTypeSettingHelper.ConstructorSettings.Order,
        KindCodeItem.Delegate => MemberTypeSettingHelper.DelegateSettings.Order,
        KindCodeItem.Destructor => MemberTypeSettingHelper.DestructorSettings.Order,
        KindCodeItem.Enum => MemberTypeSettingHelper.EnumSettings.Order,
        KindCodeItem.Event => MemberTypeSettingHelper.EventSettings.Order,
        KindCodeItem.Field => MemberTypeSettingHelper.FieldSettings.Order,
        KindCodeItem.Indexer => MemberTypeSettingHelper.IndexerSettings.Order,
        KindCodeItem.Interface => MemberTypeSettingHelper.InterfaceSettings.Order,
        KindCodeItem.Method => MemberTypeSettingHelper.MethodSettings.Order,
        KindCodeItem.Property => MemberTypeSettingHelper.PropertySettings.Order,
        KindCodeItem.Struct => MemberTypeSettingHelper.StructSettings.Order,
        _ => 0,
      };
    }

        private static int CalculateAccessOffset(BaseCodeItem codeItem)
        {
      BaseCodeItemElement codeItemElement = codeItem as BaseCodeItemElement;
            if (codeItemElement == null) return 0;

      List<vsCMAccess> itemsOrder = new()
      {
                vsCMAccess.vsCMAccessPublic,
                vsCMAccess.vsCMAccessAssemblyOrFamily,
                vsCMAccess.vsCMAccessProject,
                vsCMAccess.vsCMAccessProjectOrProtected,
                vsCMAccess.vsCMAccessProtected,
                vsCMAccess.vsCMAccessPrivate
            };

            if (Settings.Default.Reorganizing_ReverseOrderByAccessLevel)
            {
                itemsOrder.Reverse();
            }

            return itemsOrder.IndexOf(codeItemElement.Access) + 1;
        }

        private static int CalculateExplicitInterfaceOffset(BaseCodeItem codeItem)
        {
            if (Settings.Default.Reorganizing_ExplicitMembersAtEnd)
            {
        IInterfaceItem interfaceItem = codeItem as IInterfaceItem;
                if ((interfaceItem?.IsExplicitInterfaceImplementation == true))
                {
                    return 1;
                }
            }

            return 0;
        }

        private static int CalculateConstantOffset(BaseCodeItem codeItem)
        {
      CodeItemField codeItemField = codeItem as CodeItemField;
            if (codeItemField == null) return 0;

            return codeItemField.IsConstant ? 0 : 1;
        }

        private static int CalculateStaticOffset(BaseCodeItem codeItem)
        {
      BaseCodeItemElement codeItemElement = codeItem as BaseCodeItemElement;
            if (codeItemElement == null) return 0;

            return codeItemElement.IsStatic ? 0 : 1;
        }

        private static int CalculateReadOnlyOffset(BaseCodeItem codeItem)
        {
      CodeItemField codeItemField = codeItem as CodeItemField;
            if (codeItemField == null) return 0;

            return codeItemField.IsReadOnly ? 0 : 1;
        }

        private static string NormalizeName(BaseCodeItem codeItem)
        {
            string name = codeItem.Name;
      IInterfaceItem interfaceItem = codeItem as IInterfaceItem;
            if ((interfaceItem?.IsExplicitInterfaceImplementation == true))
            {
                // Try to find where the interface ends and the method starts
                int dot = name.LastIndexOf('.') + 1;
                if (0 < dot && dot < name.Length)
                {
                    return name.Substring(dot);
                }
            }

            return name;
        }

        #endregion Methods
    }
}