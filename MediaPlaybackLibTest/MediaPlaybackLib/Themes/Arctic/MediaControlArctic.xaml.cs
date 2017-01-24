using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace MediaPlaybackLib.Themes.Arctic
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix"), Export("/theming", typeof(ResourceDictionary))]
    [ExportMetadata("theme", "Arctic")]
    [ExcludeFromCodeCoverage]
    public partial class MediaControlArctic
    {
        public MediaControlArctic()
        {
            InitializeComponent();
        }
    }
}
