﻿using System;
using System.Collections.Generic;
using System.Text;
using TwitterInterface.Data.Entity;

namespace TwitterInterface.Data
{
    public class Status : BasicEntitiesGroup
    {
        //이 트윗정보를 얻기 위해 사용된 계정 아이디
        public List<long> issuer;

        public long id;

        public DateTime createdAt;
        public User creater;

        public string text;
        public bool truncated;

        public string source;

        public long replyToStatusId;
        public long replyToUserId;
        public string replyToScreenName;

        //TODO: coordinates
        //TODO: place

        public bool isQuote;
        public long quotedStatusId;
        public Status quotedStatus;

        public bool isRetweetedStatus {
            get {
                return retweetedStatus != null;
            }
        }
        public Status retweetedStatus;

        public int replyCount;
        public int retweetCount;
        public int favoriteCount;
        
        public ExtendMedia[] extendMedias;
        public Polls[] polls;

        public bool isFavortedByUser;
        public bool isRetweetedByUser;
        public Status retweetByUser;

        public bool possiblySensitive;
    }
}
