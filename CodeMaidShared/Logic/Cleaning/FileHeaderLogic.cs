using EnvDTE;
using Microsoft.VisualStudio.Shell;
using ASGV.CodeMaid.Helpers;
using ASGV.CodeMaid.Properties;
using ASGV.CodeMaid.UI.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ASGV.CodeMaid.Logic.Cleaning
{
    /// <summary>
    /// A class for encapsulating the logic of file header updates.
    /// </summary>
    internal sealed class FileHeaderLogic
    {
        #region Fields

        private readonly CodeMaidPackage _package;

        private const int HeaderMaxNbLines = 60;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// The singleton instance of the <see cref="FileHeaderLogic" /> class.
        /// </summary>
        private static FileHeaderLogic _instance;

        /// <summary>
        /// Gets an instance of the <see cref="FileHeaderLogic" /> class.
        /// </summary>
        /// <param name="package">The hosting package.</param>
        /// <returns>An instance of the <see cref="FileHeaderLogic" /> class.</returns>
        internal static FileHeaderLogic GetInstance(CodeMaidPackage package)
        {
            return _instance ??= new FileHeaderLogic(package);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHeaderLogic" /> class.
        /// </summary>
        /// <param name="package">The hosting package.</param>
        private FileHeaderLogic(CodeMaidPackage package)
        {
            _package = package;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Updates the file header for the specified text document.
        /// </summary>
        /// <param name="textDocument">The text document to update.</param>
        internal void UpdateFileHeader(TextDocument textDocument)
        {
            string settingsFileHeader = FileHeaderHelper.GetFileHeaderFromSettings(textDocument);
            if (string.IsNullOrWhiteSpace(settingsFileHeader))
            {
                return;
            }

            if (!settingsFileHeader.EndsWith(Environment.NewLine))
            {
                settingsFileHeader += Environment.NewLine;
            }
            ThreadHelper.ThrowIfNotOnUIThread();
            switch ((HeaderUpdateMode)Settings.Default.Cleaning_UpdateFileHeader_HeaderUpdateMode)
            {
                case HeaderUpdateMode.Insert:
                    InsertFileHeader(textDocument, settingsFileHeader);
                    break;

                case HeaderUpdateMode.Replace:
                    ReplaceFileHeader(textDocument, settingsFileHeader);
                    break;

                default:
                    throw new InvalidEnumArgumentException("Invalid file header update mode retrieved from settings");
            }
        }

        private int GetHeaderLength(TextDocument textDocument, bool skipUsings)
        {
            string headerBlock = ReadTextBlock(textDocument);
            CodeLanguage language = textDocument.GetCodeLanguage();

            return FileHeaderHelper.GetHeaderLength(language, headerBlock, skipUsings);
        }

        private string GetCurrentHeader(TextDocument textDocument, bool skipUsings)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            int currentHeaderLength = GetHeaderLength(textDocument, skipUsings);

            EditPoint headerBlockStart = textDocument.StartPoint.CreateEditPoint();

            if (skipUsings)
            {
                int nbLinesToSkip = GetNbLinesToSkip(textDocument);

                headerBlockStart.MoveToLineAndOffset(nbLinesToSkip + 1, 1);
            }

            return headerBlockStart.GetText(currentHeaderLength + 1).Trim();
        }

        private int GetNbLinesToSkip(TextDocument textDocument)
        {
            string docHeadBlock = ReadTextBlock(textDocument);

            return FileHeaderHelper.GetNbLinesToSkip("using ", docHeadBlock, ["namespace ", "[assembly:"]);
        }

        private void InsertFileHeader(TextDocument textDocument, string settingsFileHeader)
        {
            switch (FileHeaderHelper.GetFileHeaderPositionFromSettings(textDocument))
            {
                case HeaderPosition.DocumentStart:
                    InsertFileHeaderDocumentStart(textDocument, settingsFileHeader);
                    return;

                case HeaderPosition.AfterUsings:
                    InsertFileHeaderAfterUsings(textDocument, settingsFileHeader);
                    return;

                default:
                    throw new InvalidEnumArgumentException("Invalid file header position retrieved from settings");
            }
        }

        /// <summary>
        /// Inserts a file header located after the first block of "using" lines
        /// </summary>
        /// <param name="textDocument">The document to update</param>
        /// <param name="settingsFileHeader">The new file header read from the settings</param>
        /// <remarks>Only valid for languages containing "using" directive</remarks>
        private void InsertFileHeaderAfterUsings(TextDocument textDocument, string settingsFileHeader)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            string currentHeader = GetCurrentHeader(textDocument, true).Trim();

            if (currentHeader.StartsWith(settingsFileHeader.Trim()))
            {
                return;
            }

            EditPoint headerBlockStart = textDocument.StartPoint.CreateEditPoint();
            int nbLinesToSkip = GetNbLinesToSkip(textDocument);

            headerBlockStart.MoveToLineAndOffset(nbLinesToSkip + 1, 1);

            if (!settingsFileHeader.StartsWith(Environment.NewLine))
            {
                settingsFileHeader = Environment.NewLine + settingsFileHeader;
            }

            headerBlockStart.Insert(settingsFileHeader);
        }

        private void InsertFileHeaderDocumentStart(TextDocument textDocument, string settingsFileHeader)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            EditPoint cursor = textDocument.StartPoint.CreateEditPoint();
            string existingFileHeader = cursor.GetText(settingsFileHeader.Length);

            if (!existingFileHeader.StartsWith(settingsFileHeader.Trim()))
            {
                cursor.Insert(settingsFileHeader);
            }
        }

        /// <summary>
        /// Reads the first lines of a document
        /// </summary>
        /// <param name="textDocument">The document to read</param>
        /// <returns>A string representing the first <see cref="HeaderMaxNbLines"/> lines of the document</returns>
        private string ReadTextBlock(TextDocument textDocument)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            int maxNbLines = Math.Min(HeaderMaxNbLines, textDocument.EndPoint.Line);
            EditPoint blockStart = textDocument.StartPoint.CreateEditPoint();

            return blockStart.GetLines(1, maxNbLines);
        }

        private void ReplaceFileHeader(TextDocument textDocument, string settingsFileHeader)
        {
            switch (FileHeaderHelper.GetFileHeaderPositionFromSettings(textDocument))
            {
                case HeaderPosition.DocumentStart:
                    ReplaceFileHeaderAfterUsings(textDocument, string.Empty); // Removes header after usings if present
                    ReplaceFileHeaderDocumentStart(textDocument, settingsFileHeader);
                    return;

                case HeaderPosition.AfterUsings:
                    ReplaceFileHeaderDocumentStart(textDocument, string.Empty); // Removes header at document start if present
                    ReplaceFileHeaderAfterUsings(textDocument, settingsFileHeader);
                    return;

                default:
                    throw new InvalidEnumArgumentException("Invalid file header position retrieved from settings");
            }
        }

        /// <summary>
        /// Updates the file header located after the first block of "using" lines
        /// </summary>
        /// <param name="textDocument">The document to update</param>
        /// <param name="settingsFileHeader">The new file header read from the settings</param>
        /// <remarks>Only valid for languages containing "using" directive</remarks>
        private void ReplaceFileHeaderAfterUsings(TextDocument textDocument, string settingsFileHeader)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            string currentHeader = GetCurrentHeader(textDocument, true).Trim();
            string newHeader = settingsFileHeader.Trim();

            if (string.Equals(currentHeader, newHeader))
            {
                return;
            }

            EditPoint headerBlockStart = textDocument.StartPoint.CreateEditPoint();
            int nbLinesToSkip = GetNbLinesToSkip(textDocument);

            headerBlockStart.MoveToLineAndOffset(nbLinesToSkip + 1, 1);

            int currentHeaderLength = GetHeaderLength(textDocument, true);

            if (!settingsFileHeader.StartsWith(Environment.NewLine))
            {
                settingsFileHeader = Environment.NewLine + settingsFileHeader;
            }

            headerBlockStart.ReplaceText(currentHeaderLength, settingsFileHeader, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
        }

        private void ReplaceFileHeaderDocumentStart(TextDocument textDocument, string settingsFileHeader)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            string currentHeader = GetCurrentHeader(textDocument, false).Trim();
            string newHeader = settingsFileHeader.Trim();

            if (string.Equals(currentHeader, newHeader))
            {
                return;
            }

            EditPoint headerBlockStart = textDocument.StartPoint.CreateEditPoint();
            int currentHeaderLength = GetHeaderLength(textDocument, false);

            headerBlockStart.ReplaceText(currentHeaderLength, settingsFileHeader, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
        }

        #endregion Methods
    }
}