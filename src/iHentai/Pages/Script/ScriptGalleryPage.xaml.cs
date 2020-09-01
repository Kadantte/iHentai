﻿using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using iHentai.Services;
using iHentai.Services.Models.Script;
using iHentai.ViewModels.Script;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace iHentai.Pages.Script
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ScriptGalleryPage : Page
    {
        public ScriptGalleryPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ScriptGalleryViewModel viewModel)
            {
                this.ViewModel = viewModel;
            }
        }

        private ScriptGalleryViewModel ViewModel { get; set; }

        private void ListViewBase_OnItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ScriptGalleryModel item && ViewModel.Api is ScriptApi api)
            {
                var viewModel = new ScriptGalleryDetailViewModel(api, item);
            }
        }
    }
}