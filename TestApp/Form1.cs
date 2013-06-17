using SharpIMPP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TestApp
{
    public partial class Form1 : Form
    {
        IMPPClient si;

        ListViewGroup pendingG;
        ListViewGroup blockedG;
        ListViewGroup allowG;
        ListViewGroup contactG;
        public Form1()
        {
            InitializeComponent();
            pendingG = new ListViewGroup("Pending");
            blockedG = new ListViewGroup("Blocked");
            allowG = new ListViewGroup("Allowed");
            contactG = new ListViewGroup("Contacts");
            contactList.Groups.Add(pendingG);
            contactList.Groups.Add(blockedG);
            contactList.Groups.Add(allowG);
            contactList.Groups.Add(contactG);
        }

        private void loginButton_Click(object sender, EventArgs e)
        {

            si = new IMPPClient();
            si.Connect(usernameBox.Text, "trillian.im", passwordBox.Text);
            passwordBox.Text = "";
            si.ListReceived += si_ListReceived;
            si.ChatReceived += si_ChatReceived;
            si.ContactStatusChanged += si_ContactStatusChanged;
        }

        void si_ContactStatusChanged(object sender, IMPPClient.ContactStatusEventArgs e)
        {
            foreach (ListViewItem i in contactList.Items)
            {
                if ((string)i.Tag == e.Username)
                {
                    i.Text = e.Nick + " - " + e.StatusMessage;
                    switch (e.Status)
                    {
                        case 1:
                            i.BackColor = Color.Green;
                            break;
                        case 2:
                            i.BackColor = Color.Gray;
                            break;
                    }
                }
            }
        }

        void si_ChatReceived(object sender, IMPPClient.ChatEventArgs e)
        {
            messageList.Items.Add(e.From + ": " + e.Message);
        }

        void si_ListReceived(object sender, IMPPClient.ListEventArgs e)
        {
            Console.WriteLine("Got contacts:");
            foreach (var pair in e.ContactList)
            {
                Console.WriteLine(pair.ContactType.ToString() + ": " + pair.ContactName);
                var lvi = new ListViewItem(pair.ContactName);
                lvi.Tag = pair.ContactName;
                switch (pair.ContactType)
                {
                    case SharpIMPP.Enums.ListTypes.TTupleType.ALLOW_ADDRESS:
                        lvi.Group = allowG;
                        allowG.Items.Add(lvi);
                        break;
                    case SharpIMPP.Enums.ListTypes.TTupleType.BLOCK_ADDRESS:
                        lvi.Group = blockedG;
                        blockedG.Items.Add(lvi);
                        break;
                    case SharpIMPP.Enums.ListTypes.TTupleType.CONTACT_ADDRESS:
                        lvi.Group = contactG;
                        contactG.Items.Add(lvi);
                        break;
                    case SharpIMPP.Enums.ListTypes.TTupleType.PENDING_ADDRESS:
                        lvi.Group = pendingG;
                        pendingG.Items.Add(lvi);
                        break;
                }
                contactList.Items.Add(lvi);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            si.Disconnect();
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            si.SendChat(toLabel.Text, messageBox.Text);
        }

        private void contactList_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                toLabel.Text = (string)contactList.SelectedItems[0].Tag;
                sendButton.Enabled = true;
            }
            catch (Exception)
            {
                toLabel.Text = "-";
                sendButton.Enabled = false;
            }
        }
    }
}
