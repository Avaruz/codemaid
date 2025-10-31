using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using ASGV.CodeMaid.Helpers;
using ASGV.CodeMaid.Properties;
using ASGV.CodeMaid.UI.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ASGV.CodeMaid.UI
{
  /// <summary>
  /// A helper class for managing the active theme.
  /// </summary>
  public sealed class ThemeManager : Bindable
  {
    #region Fields

    private static Dictionary<ThemeMode, Uri> _themeUris;

    private readonly CodeMaidPackage _package;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// The singleton instance of the <see cref="ThemeManager" /> class.
    /// </summary>
    private static ThemeManager _instance;

    /// <summary>
    /// Gets an instance of the <see cref="ThemeManager" /> class.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    /// <returns>An instance of the <see cref="ThemeManager" /> class.</returns>
    internal static ThemeManager GetInstance(CodeMaidPackage package)
    {
      return _instance ??= new ThemeManager(package);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeManager" /> class.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    private ThemeManager(CodeMaidPackage package)
    {
      _package = package;
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Gets the active theme.
    /// </summary>
    public ThemeMode ActiveTheme
    {
      get { return GetPropertyValue<ThemeMode>(); }
      private set { SetPropertyValue(value); }
    }

    /// <summary>
    /// Gets the dictionary of Uris corresponding to <see cref="ThemeMode" /> values.
    /// </summary>
    private static Dictionary<ThemeMode, Uri> ThemeUris
    {
      get
      {
        if (_themeUris == null)
        {
          _themeUris = [];

          foreach (ThemeMode theme in Enum.GetValues(typeof(ThemeMode)))
          {
            string uriString = $"/UI/Themes/CodeMaid{theme}Theme.xaml";
            Uri uri = new(uriString, UriKind.Relative);

            _themeUris.Add(theme, uri);
          }
        }

        return _themeUris;
      }
    }

    /// <summary>
    /// Gets the Spade content as a FrameworkElement, may be null.
    /// </summary>
    private FrameworkElement SpadeContent => _package.Spade?.Content as FrameworkElement;

    #endregion Properties

    #region Methods

    /// <summary>
    /// Applies the appropriate theme based on current settings.
    /// </summary>
    public void ApplyTheme()
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      ActiveTheme = ResolveActiveTheme();

      ApplyThemeToElement(SpadeContent, ActiveTheme);
    }

    /// <summary>
    /// Resolves which theme should currently be active based on settings.
    /// </summary>
    /// <returns>The resolved theme.</returns>
    private ThemeMode ResolveActiveTheme()
    {
      ThemeMode theme = (ThemeMode)Settings.Default.General_Theme;

      return theme == ThemeMode.AutoDetect ? AutoDetectTheme() : theme;
    }

    /// <summary>
    /// Auto-detects which theme should be active based on the current IDE settings.
    /// </summary>
    private ThemeMode AutoDetectTheme()
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      const int medianColor = 128 * 3;
      Color bgColor = GetColorFromUInt(_package.IDE.GetThemeColor(vsThemeColors.vsThemeColorToolWindowBackground));

      return (bgColor.R + bgColor.G + bgColor.B) >= medianColor ? ThemeMode.Light : ThemeMode.Dark;
    }

    /// <summary>
    /// A simple converter for turning a <see cref="uint" /> into a <see cref="Color" />.
    /// </summary>
    /// <param name="number">The number to convert.</param>
    /// <returns>The color.</returns>
    private static Color GetColorFromUInt(uint number)
    {
      return Color.FromRgb((byte)(number >> 16),
                           (byte)(number >> 8),
                           (byte)(number >> 0));
    }

    /// <summary>
    /// Applies the specified theme to the specified element.
    /// </summary>
    /// <param name="element">The element to theme.</param>
    /// <param name="theme">The theme to apply.</param>
    private void ApplyThemeToElement(FrameworkElement element, ThemeMode theme)
    {
      if (element == null)
      {
        return;
      }

      element.Resources ??= [];

      // Search to see if the theme is already applied, or if other themes need removed.
      foreach (ResourceDictionary mergedDictionary in element.Resources.MergedDictionaries.ToList())
      {
        if (mergedDictionary.Source == ThemeUris[theme])
        {
          // Theme is already applied, no further processing necessary.
          return;
        }

        if (ThemeUris.ContainsValue(mergedDictionary.Source))
        {
          // Remove any other theme dictionaries.
          element.Resources.MergedDictionaries.Remove(mergedDictionary);
        }
      }

      // Apply the theme.
      ResourceDictionary resourceDictionary = LoadResourceDictionary(ThemeUris[theme]);
      if (resourceDictionary != null)
      {
        element.Resources.MergedDictionaries.Insert(0, resourceDictionary);
      }
    }

    /// <summary>
    /// Attempts to load a resource dictionary for the specified theme URI.
    /// </summary>
    /// <param name="themeUri">The theme URI.</param>
    /// <returns>The loaded resource dictionary, otherwise null.</returns>
    private ResourceDictionary LoadResourceDictionary(Uri themeUri)
    {
      try
      {
        ResourceDictionary dictionary = (ResourceDictionary)Application.LoadComponent(themeUri);
        dictionary.Source = themeUri;

        return dictionary;
      }
      catch (Exception ex)
      {
        OutputWindowHelper.ExceptionWriteLine($"Unable to load theme '{themeUri}'", ex);
        ThreadHelper.ThrowIfNotOnUIThread();
        _package.IDE.StatusBar.Text = string.Format(Resources.CodeMaidFailedToLoadTheme0SeeOutputWindowForMoreDetails, themeUri);

        return null;
      }
    }

    #endregion Methods
  }
}