﻿using FFImageLoading.Forms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using DataStatus = TwitterInterface.Data.Status;
using FFImageLoading.Transformations;

namespace TweetTail.Status
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StatusCell : ViewCell
    {
        private static TintTransformation retweetTransformation = new TintTransformation("#009900");
        private static TintTransformation favoriteTransformation = new TintTransformation("#FF0000");

        private DataStatus status {
            get {
                return BindingContext as DataStatus;
            }
        }

        private ObservableCollection<DataStatus> statuses {
            get {
                return (Parent as StatusListView).Items;
            }
        }

        private CachedImage getMediaView(int inx)
        {
            switch (inx)
            {
                case 0:
                    return imgMedia1;
                case 1:
                    return imgMedia2;
                case 2:
                    return imgMedia3;
                case 3:
                    return imgMedia4;
            }
            throw new IndexOutOfRangeException();
        }

        public StatusCell()
        {
            InitializeComponent();


            imgReply.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    Application.Current.MainPage.DisplayAlert("TODO", "Reply Button", "OK");
                }),
                NumberOfTapsRequired = 1
            });

            imgRetweet.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                     //TODO: Select account when multiple issuer
                     try
                    {
                        await App.tail.twitter.RetweetStatus(App.tail.account.getAccountGroup(status.issuer[0]).accountForWrite, status.id);
                        getDisplayStatus(status).isRetweetedByUser = true;
                        UpdateButton();
                    }
                    catch (Exception e)
                    {

                    }

                }),
                NumberOfTapsRequired = 1
            });

            imgFavorite.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    //TODO: Select account when multiple issuer
                    try
                    {
                        await App.tail.twitter.CreateFavorite(App.tail.account.getAccountGroup(status.issuer[0]).accountForWrite, status.id);
                        getDisplayStatus(status).isFavortedByUser = true;
                        UpdateButton();
                    }
                    catch (Exception e)
                    {

                    }

                }),
                NumberOfTapsRequired = 1
            });

            imgDelete.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    var group = App.tail.account.getAccountGroup(status.creater.id);
                    if (group != null)
                    {
                        if( await Application.Current.MainPage.DisplayAlert("제거 확인", "이 트윗이 제거됩니다, 진행합니까?", "네", "아니오") )
                        {
                            try
                            {
                                await App.tail.twitter.DestroyStatus(group.accountForWrite, status.id);
                            }
                            catch (Exception e)
                            {
                                await Application.Current.MainPage.DisplayAlert("오류", e.Message, "확인");
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    statuses.Remove(status);
                }),
                NumberOfTapsRequired = 1
            });

            imgMore.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    Application.Current.MainPage.DisplayAlert("TODO", "More Button", "OK");
                }),
                NumberOfTapsRequired = 1
            });

            for (int i = 0; i < 4; i++)
            {
                int inx = i; //Value Copy
                getMediaView(i).GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() =>
                    {
                        App.Navigation.PushAsync(new MediaPage(getDisplayStatus(status), inx));
                    }),
                    NumberOfTapsRequired = 1
                });
            }
        }

        private DataStatus getDisplayStatus(DataStatus status)
        {
            if (status.retweetedStatus != null)
            {
                return status.retweetedStatus;
            }
            else
            {
                return status;
            }
        }

        protected void UpdateImage()
        {
            var display = getDisplayStatus(status);

            imgProfile.Source = null;
            for (int i = 0; i < 4; i++)
            {
                getMediaView(i).Source = null;
            }

            imgProfile.Source = display.creater.profileHttpsImageURL;

            if (display.extendMedias != null)
            {
                for (int i = 0; i < display.extendMedias.Length; i++)
                {
                    getMediaView(i).Source = display.extendMedias[i].mediaURLHttps;
                }
            }
        }

        protected void UpdateButton()
        {
            var display = getDisplayStatus(status);

            imgRetweet.Transformations.Clear();
            if (display.isRetweetedByUser)
            {
                imgRetweet.Transformations.Add(retweetTransformation);
            }

            imgFavorite.Transformations.Clear();
            if (display.isFavortedByUser)
            {
                imgFavorite.Transformations.Add(favoriteTransformation);
            }
            imgRetweet.ReloadImage();
            imgFavorite.ReloadImage();

            var group = App.tail.account.getAccountGroup( status.creater.id );
            if(group != null)
            {
                imgDelete.Source = "ic_delete_black_24dp";
            }
            else
            {
                imgDelete.Source = "ic_visibility_off_black_24dp";
            }
        }

        protected void Update()
        {
            if (BindingContext is DataStatus) { }
            else
            {
                return;
            }
            var status = BindingContext as DataStatus;
            var display = getDisplayStatus(status);

            if (display != status)
            {
                viewHeader.IsVisible = true;
                lblHeader.Text = string.Format("{0} 님이 리트윗 하셨습니다", status.creater.nickName);
            }
            else
            {
                viewHeader.IsVisible = false;
            }

            imgLock.IsVisible = display.creater.isProtected;
            lblCreatedAt.Text = display.createdAt.ToString();
            lblName.Text = string.Format("{0} @{1}", display.creater.nickName, display.creater.screenName);
            lblText.Text = display.text;

            imgProfile.Source = null;
            for (int i = 0; i < 4; i++)
            {
                getMediaView(i).Source = null;
            }
            if (display.extendMedias != null)
            {
                viewMedias.IsVisible = true;
            }
            else
            {
                viewMedias.IsVisible = false;
            }
            UpdateImage();
            UpdateButton();
        }

        protected override void OnBindingContextChanged()
        {
            Update();
            base.OnBindingContextChanged();
        }
    }
}