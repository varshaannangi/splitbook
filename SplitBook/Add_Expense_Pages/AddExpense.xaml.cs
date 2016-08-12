﻿using SplitBook.Controller;
using SplitBook.Model;
using SplitBook.Utilities;
using SplitBook.Views;
using System;
using System.ComponentModel;
using System.Net;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace SplitBook.Add_Expense_Pages
{
    public sealed partial class AddExpense : Page
    {
        BackgroundWorker addExpenseBackgroundWorker;

        public AddExpense()
        {
            this.InitializeComponent();
            BackButton.Tapped += BackButton_Tapped;

            addExpenseBackgroundWorker = new BackgroundWorker();
            addExpenseBackgroundWorker.WorkerSupportsCancellation = true;
            addExpenseBackgroundWorker.DoWork += new DoWorkEventHandler(addExpenseBackgroundWorker_DoWork);

            this.expenseControl.amountSplit = AmountSplit.EqualSplit;
            this.expenseControl.groupList.SelectionChanged += groupListPicker_SelectionChanged;

            //This helps to auto-populate if the user is coming from the GroupDetails or UserDetails page
            autoPopulateGroup();
            autoPopulateExpenseShareUsers();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            GoogleAnalytics.EasyTracker.GetTracker().SendView("AddExpensePage");
            base.OnNavigatedTo(e);
        }

        private void BackButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        private void autoPopulateGroup()
        {
            if (this.expenseControl.expense == null)
                return;
            else
            {
                foreach (var group in expenseControl.groupsList)
                {
                    if (this.expenseControl.expense.group_id == group.id)
                        this.expenseControl.groupList.SelectedItem = group;
                }
            }
        }

        private void autoPopulateExpenseShareUsers()
        {
            if (this.expenseControl.expense == null)
                return;
            else
            {
                foreach (var user in expenseControl.friends)
                {
                    if (this.expenseControl.expense.specificUserId == user.user_id)
                        this.expenseControl.friendList.SelectedItems.Add(user);
                }
            }
        }

        private async void btnOkay_Click(object sender, RoutedEventArgs e)
        {
            commandBar.IsEnabled = false;
            bool proceed = await this.expenseControl.setupExpense();
            if (addExpenseBackgroundWorker.IsBusy != true)
            {
                busyIndicator.IsActive = true;
                if (proceed)
                    addExpenseBackgroundWorker.RunWorkerAsync();
                else
                {
                    busyIndicator.IsActive = false;
                    commandBar.IsEnabled = true;
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            (Application.Current as App).ADD_EXPENSE = null;

            //This page might be accessed from the start screen tile and hence canGoBack might be false
            if (this.Frame.CanGoBack)
                this.Frame.Navigate(typeof(FriendsPage));
            else
                Application.Current.Exit();
        }

        //private void btnPin_Click(object sender, EventArgs e)
        //{
        //    StandardTileData tileData = new StandardTileData
        //    {
        //        Title = "add expense",
        //        BackgroundImage = new Uri("/Assets/Tiles/TileMediumAdd.png", UriKind.Relative),
        //    };

        //    Uri tileUri = new Uri(Constants.ADD_EXPENSE_TILE_SHORTCUT, UriKind.Relative);
        //    ShellTile.Create(tileUri, tileData);
        //}

        //returns true only if one or less group is selected
        private bool validGroupSelected()
        {
            if (this.expenseControl.groupList.SelectedItems.Count > 1)
                return false;
            else
                return true;
        }

        private async void groupListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.expenseControl.expense.group_id = 0;
            if (this.expenseControl.groupList.SelectedItem == null)
                return;

            if (validGroupSelected())
            {
                Group selectedGroup = this.expenseControl.groupList.SelectedItem as Group;
                this.expenseControl.expense.group_id = selectedGroup.id;

                //clear all the previoulsy selected friends.
                this.expenseControl.friendList.SelectedItems.Clear();

                foreach (var member in selectedGroup.members)
                {
                    //you don't need to add yourself as you will be added by default.
                    if (member.id == App.currentUser.id || this.expenseControl.friendList.SelectedItems.Contains(member))
                        continue;
                    this.expenseControl.friendList.SelectedItems.Add(new Expense_Share() { user = member, user_id = member.id });
                }
            }

            else
            {
                var dialog = new MessageDialog("Sorry you can only select one group")
                {
                    Title = "Error"
                };
                await dialog.ShowAsync();

                //this.friendList.SelectedItems.Clear();
                this.expenseControl.groupList.SelectedItems.Clear();
            }
        }

        private void addExpenseBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            ModifyDatabase modify = new ModifyDatabase(_addExpenseCompleted);
            modify.addExpense(this.expenseControl.expense);
        }

        private async void _addExpenseCompleted(bool success, HttpStatusCode errorCode)
        {
            if (success)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    (Application.Current as App).ADD_EXPENSE = null;
                    busyIndicator.IsActive = false;
                    if (MainPage.syncDatabaseBackgroundWorker.IsBusy != true)
                    {
                        MainPage.syncDatabaseBackgroundWorker.RunWorkerAsync();
                    }
                    this.Frame.Navigate(typeof(FriendsPage));
                });
            }
            else
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    busyIndicator.IsActive = false;

                    if (errorCode == HttpStatusCode.Unauthorized)
                    {
                        Helpers.logout();
                        MainPage.Current.Frame.Navigate(typeof(LoginPage));
                    }
                    else
                    {
                        var dialog = new MessageDialog("Unable to add expense")
                        {
                            Title = "Error"
                        };
                        await dialog.ShowAsync();
                    }
                    commandBar.IsEnabled = true;
                });
            }
        }
    }
}