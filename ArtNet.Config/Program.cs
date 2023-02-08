using System.Text;
using ArtNet.Config.Pages;

namespace ArtNet.Config; 

class Program {
    static async Task Main(string[] args) {
        Console.OutputEncoding = Encoding.UTF8;
        IPage.ActivePage = new DeviceEnumeration(null);

        while (IPage.ActivePage != null) {
            IPage.ActivePage.Render();
            var newPage = IPage.ActivePage.HandleInput();

            if (newPage != IPage.ActivePage) {
                IPage.ActivePage.ExitPage();
                if (newPage != null) {
                    newPage.EnterPage();
                }
            }

            IPage.ActivePage = newPage;
        }

        Console.ReadLine();
    }
}