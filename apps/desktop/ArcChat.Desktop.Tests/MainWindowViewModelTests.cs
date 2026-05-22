using ArcChat.Desktop.ViewModels;
using Xunit;

namespace ArcChat.Desktop.Tests
{
    public sealed class MainWindowViewModelTests
    {
        [Fact]
        public void GreetingReturnsProductName()
        {
            MainWindowViewModel viewModel = new();

            Assert.Equal("ArcChat", viewModel.Greeting);
        }
    }
}
