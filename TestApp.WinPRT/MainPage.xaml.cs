using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TestApp.WinPRT.Resources;
using SharpIMPP;

namespace TestApp.WinPRT
{
    public partial class MainPage : PhoneApplicationPage
    {
        IMPPClient ic;
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
            contactList.Items.Add("test item");
        }

        private void loginButton_Click(object sender, RoutedEventArgs e)
        {
            ic = new IMPPClient();
            ic.Connect(usernameBox.Text, "trillian.im", passwordBox.Password);
            passwordBox.Password = "";
            ic.ChatReceived += ic_ChatReceived;
            ic.ContactStatusChanged += ic_ContactStatusChanged;
            ic.ListReceived += ic_ListReceived;
            ic.ContactTyping += ic_ContactTyping;
        }

        void ic_ContactTyping(object sender, IMPPClient.TypingEventArgs e)
        {
            Action a = new Action(() =>
            {
                if (e.IsTyping)
                {
                    //typingIndicatorBlock.Text = e.From + " is typing";
                }
                else
                {
                    //typingIndicatorBlock.Text = "";
                }
            });
            if (Dispatcher == null)
            {
                a();
            }
            else
            {
                Dispatcher.BeginInvoke(a);
            }
        }

        void ic_ListReceived(object sender, IMPPClient.ListEventArgs e)
        {
            Action a = new Action( () =>
            {
                foreach (var pair in e.ContactList)
                {
                    contactList.Items.Add(pair.ContactName);
                }
            });
            if (Dispatcher == null)
            {
                a();
            }
            else
            {
                Dispatcher.BeginInvoke(a);
            }
        }

        void ic_ContactStatusChanged(object sender, IMPPClient.ContactStatusEventArgs e)
        {
            Action a = new Action(() =>
            {
            });

            if (Dispatcher == null)
            {
                a();
            }
            else
            {
                Dispatcher.BeginInvoke(a);
            }
        }

        void ic_ChatReceived(object sender, IMPPClient.ChatEventArgs e)
        {
            Action a = new Action(() =>
            {
                //chatList.Items.Add(e.From + ": " + e.Message);
            });
            if (Dispatcher == null)
            {
                a();
            }
            else
            {
                Dispatcher.BeginInvoke(a);
            }
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}