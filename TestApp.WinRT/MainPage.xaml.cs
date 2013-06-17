using SharpIMPP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace TestApp.WinRT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        IMPPClient ic;
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void loginButton_Click(object sender, RoutedEventArgs e)
        {
            ic = new IMPPClient();
            ic.Connect(usernameBox.Text, "trillian.im", passwordBox.Password);
            passwordBox.Password = "";
            ic.ChatReceived += ic_ChatReceived;
            ic.ContactStatusChanged += ic_ContactStatusChanged;
            ic.ListReceived += ic_ListReceived;
        }

        void ic_ListReceived(object sender, IMPPClient.ListEventArgs e)
        {
            this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                foreach (var pair in e.ContactList)
                {
                    contactList.Items.Add(pair.ContactName);
                }
            });
        }

        void ic_ContactStatusChanged(object sender, IMPPClient.ContactStatusEventArgs e)
        {
            this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
            });

        }

        void ic_ChatReceived(object sender, IMPPClient.ChatEventArgs e)
        {
            this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                chatList.Items.Add(e.From + ": " + e.Message);
            });
        }
    }
}
