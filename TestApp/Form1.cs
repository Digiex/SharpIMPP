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
        public Form1()
        {
            InitializeComponent();
        }

        private void loginButton_Click(object sender, EventArgs e)
        {

            si = new IMPPClient();
            si.Connect(usernameBox.Text, "trillian.im", passwordBox.Text);
            passwordBox.Text = "";
            si.ListReceived += si_ListReceived;
        }

        void si_ListReceived(object sender, IMPPClient.ListEventArgs e)
        {
            Console.WriteLine("Got contacts:");
            ListViewGroup pendingG = new ListViewGroup("Pending");
            ListViewGroup blockedG = new ListViewGroup("Blocked");
            ListViewGroup allowG = new ListViewGroup("Allowed");
            ListViewGroup contactG = new ListViewGroup("Contacts");
            foreach (var pair in e.ContactList)
            {
                Console.WriteLine(pair.ContactType.ToString() + ": " + pair.ContactName);
                var lvi = new ListViewItem(pair.ContactName);
                switch (pair.ContactType)
                {
                    case SharpIMPP.Enums.ListTypes.TTupleType.ALLOW_ADDRESS:
                        lvi.Group = allowG;
                        break;
                    case SharpIMPP.Enums.ListTypes.TTupleType.BLOCK_ADDRESS:
                        lvi.Group = blockedG;
                        break;
                    case SharpIMPP.Enums.ListTypes.TTupleType.CONTACT_ADDRESS:
                        lvi.Group = contactG;
                        break;
                    case SharpIMPP.Enums.ListTypes.TTupleType.PENDING_ADDRESS:
                        lvi.Group = pendingG;
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
                toLabel.Text = contactList.SelectedItems[0].Text;
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
