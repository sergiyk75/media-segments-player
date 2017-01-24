using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace MediaPlaybackLib.Themes.Midnight
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix"), Export("/theming", typeof(ResourceDictionary))]
    [ExportMetadata("theme", "Midnight")]
    [ExcludeFromCodeCoverage]
    public partial class MediaControlMidnight
    {
        public MediaControlMidnight()
        {
            InitializeComponent();
        }
    }
}
