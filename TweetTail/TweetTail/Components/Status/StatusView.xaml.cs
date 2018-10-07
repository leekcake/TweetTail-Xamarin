﻿using FFImageLoading.Forms;
using FFImageLoading.Transformations;
using Library.Container.Account;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TweetTail.Utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using DataUser = TwitterInterface.Data.User;
using DataStatus = TwitterInterface.Data.Status;
using TweetTail.Pages.Status;
using TweetTail.Pages.User;

namespace TweetTail.Components.Status
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StatusView : ContentView
    {
        private DataStatus status {
            get {
                return BindingContext as DataStatus;
            }
        }

        public ObservableCollection<DataStatus> statuses {
            get {
                if (Parent == null)
                {
                    return null;
                }
                if (Parent is StatusCell)
                {
                    if (Parent.Parent is StatusListView)
                    {
                        return (Parent.Parent as StatusListView).Items;
                    }
                }
                return null;
            }
        }

        private GridImageWrapper gridImageWrapper;

        private StatusView quoteView;
        
        public StatusView() : this(true)
        {

        }

        public StatusView(bool hasQuoteView)
        {
            InitializeComponent();
            gridImageWrapper = new GridImageWrapper(gridMedias);

            viewRoot.GestureRecognizers.Add(new TapGestureRecognizer()
            {
                Command = new Command(() =>
                {
                    if (status == null) return;
                    App.Navigation.PushAsync(new StatusExpandPage( getDisplayStatus(status) ));
                })
            });

            if(hasQuoteView)
            {
                quoteView = new StatusView(false);
                viewQuoteStore.Content = (quoteView);
            }

            imgProfile.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    //NotificationCell reuse
                    if(BindingContext is DataUser)
                    {
                        var user = BindingContext as DataUser;
                        App.Navigation.PushAsync(new UserDetailPage(user, App.tail.account.getAccountGroup(user.issuer[0]).accountForRead));
                        return;
                    }
                    App.Navigation.PushAsync(new UserDetailPage(getDisplayStatus(status).creater, App.tail.account.getAccountGroup(status.issuer[0]).accountForRead));
                }),
                NumberOfTapsRequired = 1
            });

            imgReply.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    var page = new ContentPage() { Style = (Style) Application.Current.Resources["backgroundStyle"] };
                    var selected = await Util.SelectAccount("어떤 계정으로 답글을 작성할까요?", status.issuer);
                    if(selected == null)
                    {
                        return;
                    }
                    var view = new StatusWriterView( selected ) { BindingContext = page };
                    view.SetReplyStatus(status);

                    page.Content = view;
                    page.Title = "트윗작성";
                    App.Navigation.PushAsync(page);
                }),
                NumberOfTapsRequired = 1
            });

            imgRetweet.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    try
                    {
                        var selected = await Util.SelectAccount("리트윗할 계정을 선택하세요", status.issuer);
                        if (selected == null)
                        {
                            return;
                        }

                        await App.tail.twitter.RetweetStatus(selected.accountForWrite, status.id);
                        getDisplayStatus(status).isRetweetedByUser = true;
                        UpdateButton();
                    }
                    catch (Exception e)
                    {
                        Util.HandleException(e);
                    }

                }),
                NumberOfTapsRequired = 1
            });

            imgFavorite.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    try
                    {
                        var selected = await Util.SelectAccount("관심글할 계정을 선택하세요", status.issuer);
                        if (selected == null)
                        {
                            return;
                        }

                        await App.tail.twitter.CreateFavorite(selected.accountForWrite, status.id);
                        getDisplayStatus(status).isFavortedByUser = true;
                        UpdateButton();
                    }
                    catch (Exception e)
                    {
                        Util.HandleException(e);
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
                        if (await Application.Current.MainPage.DisplayAlert("제거 확인", "이 트윗이 제거됩니다, 진행합니까?", "네", "아니오"))
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
                Command = new Command(async () =>
                {
                    try
                    {
                        DataStatus target = status;
                        if (status.retweetedStatus != null)
                        {
                            if (!await Application.Current.MainPage.DisplayAlert("리트윗된 트윗", "이 트윗은 다른 유저가 리트윗한 트윗입니다. 어떤 트윗을 사용합니까?" +
                                "이 트윗을 사용해 다른 계정에서 리트윗/마음을 찍는경우 리트윗한 사람에게까지 알림이 갈 수 있습니다", "이 트윗", "원본트윗"))
                            {
                                target = status.retweetedStatus;
                            }
                        }

                        string[] moreActionSheet = {
                            "다른 계정으로 리트윗",
                            "다른 계정으로 관심글"
                        };
                        var selected = await Application.Current.MainPage.DisplayActionSheet("이 트윗으로...", "취소", null, moreActionSheet);
                        AccountGroup account;
                        switch(selected)
                        {
                            case "다른 계정으로 리트윗":
                                account = await Util.SelectAccount("리트윗할 계정을 선택하세요");
                                if (account == null)
                                {
                                    return;
                                }

                                await App.tail.twitter.RetweetStatus(account.accountForWrite, target.id);
                                break;
                            case "다른 계정으로 관심글":
                                account = await Util.SelectAccount("관심글할 계정을 선택하세요");
                                if (account == null)
                                {
                                    return;
                                }

                                await App.tail.twitter.CreateFavorite(account.accountForWrite, target.id);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Util.HandleException(e);
                    }
                }),
                NumberOfTapsRequired = 1
            });

            for (int i = 0; i < 4; i++)
            {
                int inx = i; //Value Copy
                gridImageWrapper[i].GestureRecognizers.Add(new TapGestureRecognizer
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

        protected void ClearImage()
        {
            imgProfile.Source = null;
            for (int i = 0; i < 4; i++)
            {
                gridImageWrapper[i].Source = null;
            }
        }

        protected void UpdateImage()
        {
            ClearImage();

            var display = getDisplayStatus(status);

            imgProfile.Source = display.creater.profileHttpsImageURL;

            if (display.extendMedias != null)
            {
                for (int i = 0; i < display.extendMedias.Length; i++)
                {
                    gridImageWrapper[i].Source = display.extendMedias[i].mediaURLHttps + ":thumb";
                }
                gridImageWrapper.setCount(display.extendMedias.Length);
            }
        }

        protected void UpdateButton()
        {
            var display = getDisplayStatus(status);
            
            if (display.isRetweetedByUser)
            {
                imgRetweet.Source = "ic_repeat_green_300_24dp";
            }
            else
            {
                imgRetweet.Source = "ic_repeat_grey_500_24dp";
            }
            
            if (display.isFavortedByUser)
            {
                imgFavorite.Source = "ic_grade_yellow_light_24dp";
            }
            else
            {
                imgFavorite.Source = "ic_grade_grey_500_24dp";
            }

            if (statuses != null)
            {
                imgDelete.IsVisible = true;
                var group = App.tail.account.getAccountGroup(status.creater.id);
                if (group != null)
                {
                    imgDelete.Source = "ic_delete_grey_500_24dp";
                }
                else
                {
                    imgDelete.Source = "ic_visibility_off_grey_500_24dp";
                }
            }
            else
            {
                imgDelete.IsVisible = false;
            }
        }

        public void Update()
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
            lblCreatedAt.Text = display.createdAt.ToLocalTime().ToString();
            lblName.Text = string.Format("{0} @{1}", display.creater.nickName, display.creater.screenName);
            lblText.FormattedText = TwitterFormater.ParseFormattedString(display);

            imgProfile.Source = null;
            for (int i = 0; i < 4; i++)
            {
                gridImageWrapper[i].Source = null;
            }
            if (display.extendMedias != null)
            {
                gridMedias.IsVisible = true;
            }
            else
            {
                gridMedias.IsVisible = false;
            }

            viewIssuer.BindingContext = status.issuer;
            viewIssuer.Update();
            UpdateImage();
            UpdateButton();

            if(quoteView != null)
            {
                if (display.quotedStatus != null)
                {
                    quoteView.BindingContext = display.quotedStatus;
                    quoteView.Update();
                    viewQuoteStore.IsVisible = true;
                }
                else
                {
                    quoteView.ClearImage();
                    viewQuoteStore.IsVisible = false;
                }
            }
        }
    }
}