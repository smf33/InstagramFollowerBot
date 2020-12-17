using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;

namespace InstagramFollowerBot
{
    public partial class FollowerBot
    {
        private void AddForced(string configName, string configValue, Queue<string> queue)
        {
            if (!string.IsNullOrWhiteSpace(configValue))
            {
                foreach (string s in configValue.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (s.StartsWith(Config.UrlRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        if (queue.Contains(s))
                        {
                            queue.Enqueue(s);
                        }
                        else
                        {
                            Log.LogDebug("{0} useless for {1}", configName, s);
                        }
                    }
                    else
                    {
                        Log.LogWarning("Check {0} Url format for {1}", configName, s);
                    }
                }
            }
        }

        private void UserLogging()
        {
            if (Data.UserContactUrl == null
                || !TryAuthCookies())
            {
                AuthLogin();
            }
            Log.LogInformation("Logged user :  {0}", Data.UserContactUrl);
            PostAuthInit();
        }

        private bool TryAuthCookies()
        {
            if (Data.Cookies != null && Data.Cookies.Any())
            {
                MoveToWait(Config.UrlRoot);
                Selenium.Cookies = Data.Cookies; // need to have loaded the page 1st
                Selenium.SessionStorage = Data.SessionStorage; // need to have loaded the page 1st
                Selenium.LocalStorage = Data.LocalStorage; // need to have loaded the page 1st

                // Ignore the message bar : Allow Instagram Cookies
                ClickWaitIfPresent(Config.CssCookiesWarning);

                MoveToWait(Config.UrlRoot);
                // Ignore the enable notification on your browser modal popup
                ClickWaitIfPresent(Config.CssLoginWarning);

                //check cookie auth OK :  who am i ?
                ClickWaitIfPresent(Config.CssLoginMyself);
                // else if not present, will not fail and next test will detect an error and go for the normal loggin

                string curUserContactUrl = Selenium.Url;
                if (curUserContactUrl != null && curUserContactUrl.EndsWith('/')) // standardize
                {
                    curUserContactUrl = curUserContactUrl.Remove(curUserContactUrl.Length - 1);
                }

                if (Data.UserContactUrl.Equals(curUserContactUrl, StringComparison.OrdinalIgnoreCase))
                {
                    Log.LogDebug("User authentified from cookie.");
                    return true;
                }
                else
                {
                    Log.LogWarning("Couldn't authenticate user from saved cookie.");
                    return false;
                }
            }
            else
            {
                Log.LogDebug("Cookie authentification not used.");
                return false;
            }
        }

        private void AuthLogin()
        {
            MoveToWait(Config.UrlLogin);

            if (!string.IsNullOrWhiteSpace(Config.BotUserEmail))
            {
                // Ignore the message bar : Allow Instagram Cookies
                ClickWaitIfPresent(Config.CssCookiesWarning);

                Selenium.InputWrite(Config.CssLoginEmail, Config.BotUserEmail);

                if (!string.IsNullOrWhiteSpace(Config.BotUserPassword))
                {
                    Selenium.InputWrite(Config.CssLoginPassword, Config.BotUserPassword);
                    Selenium.EnterKey(Config.CssLoginPassword);
                }
                else
                {
                    Log.LogWarning("Waiting user manual password validation...");
                }

                WaitUrlStartsWith(Config.UrlRoot); // loading may take some time
                WaitHumanizer(); // after WaitUrlStartsWith because 1st loading may take extra time

                // Humain user need to login with email code check ? In this case, remove password from the config and increase a lot step time (~1min) in order to allow you to pass throu this check process
                Selenium.CrashIfPresent(Config.CssLoginUnusual, "Unusual Login Attempt Detected");

                // Confirm save user info
                ClickWaitIfPresent(Config.CssLoginSageInfo);

                // Ignore the enable notification on your browser modal popup
                ClickWaitIfPresent(Config.CssLoginWarning);

                // who am i ?
                ClickWait(Config.CssLoginMyself); // must be here, else the auth have failed
                Data.UserContactUrl = Selenium.Url;
                if (Data.UserContactUrl.EndsWith('/')) // standardize
                {
                    Data.UserContactUrl = Data.UserContactUrl.Remove(Data.UserContactUrl.Length - 1);
                }
                Data.CookiesInitDate = DateTime.UtcNow;
            }
            else
            {
                throw new FormatException("BotUserEmail required !");
            }
        }

        private void PostAuthInit()
        {
            if (!Data.MyContactsUpdate.HasValue
                || Config.BotCacheTimeLimitHours <= 0
                || DateTime.UtcNow > Data.MyContactsUpdate.Value.AddHours(Config.BotCacheTimeLimitHours))
            {
                MoveToWait(Data.UserContactUrl);

                ClickWait(Config.CssContactsFollowing);

                // ScroolDown
                SchroolDownWaitLoop(Config.CssContactsListScrollable);   // TOFIX : will crash if no contact at all
                Data.MyContacts = Selenium.GetAttributes(Config.UrlContacts)
                    .ToHashSet();
                ClickWait(Config.CssContactsListClose);

                Log.LogDebug("MyContacts ={0}", Data.MyContacts.Count);

                Data.MyContactsUpdate = DateTime.UtcNow;
            }

            AddForced("AddContactsToFollow", Config.AddContactsToFollow, Data.ContactsToFollow);
            AddForced("AddPhotosToLike", Config.AddPhotosToLike, Data.PhotosToLike);
        }

        private void DetectContactsFollowBack()
        {
            MoveToWait(Data.UserContactUrl);

            ClickWait(Config.CssContactsFollowers);

            SchroolDownWaitLoop(Config.CssContactsListScrollable);
            IEnumerable<string> list = Selenium.GetAttributes(Config.UrlContacts)
                                      .ToList(); // Solve
            ClickWait(Config.CssContactsListClose);

            int c = Data.ContactsToFollow.Count;
            foreach (string needToFollow in list
                .Except(Data.MyContacts)
                .Except(Data.MyContactsBanned)
                .Except(Data.ContactsToFollow))
            {
                Data.ContactsToFollow.Enqueue(needToFollow);
            }
            Log.LogDebug("$ContactsToFollow +{0}", Data.ContactsToFollow.Count - c);
        }

        private void ExplorePeople()
        {
            MoveToWait(Config.UrlExplorePeople);

            SchroolDownWaitLoop(Config.BotExplorePeopleScrools);

            IEnumerable<string> list = Selenium.GetAttributes(Config.CssSuggestedContact)
                .ToList(); // Solve the request

            int c = Data.ContactsToFollow.Count;
            foreach (string needToFollow in list
                .Except(Data.MyContacts)
                .Except(Data.MyContactsBanned)
                .Except(Data.ContactsToFollow))
            {
                Data.ContactsToFollow.Enqueue(needToFollow);
            }
            Log.LogDebug("$ContactsToFollow +{0}", Data.ContactsToFollow.Count - c);
        }

        private void ExplorePhotos()
        {
            MoveToWait(Config.UrlExplorePhotos);

            SchroolDownWaitLoop(Config.BotExplorePhotosPageInitScrools);

            IEnumerable<string> list = Selenium.GetAttributes(Config.CssExplorePhotos, "href", false, true)
                .ToList(); // Solve the request

            if (list.Any())
            {
                int c = Data.PhotosToLike.Count;
                foreach (string url in list
                    .Except(Data.PhotosToLike))
                {
                    Data.PhotosToLike.Enqueue(url);
                }
                Log.LogDebug("$PhotosToLike +{0}", Data.PhotosToLike.Count - c);
            }
            else
            {
                Log.LogWarning("Exploring with CSS \"{0}\" return nothing ! A CSS selector update may be required.", Config.CssExplorePhotos);
            }
        }

        private void DetectContactsUnfollowBack()
        {
            MoveToWait(Data.UserContactUrl);

            ClickWait(Config.CssContactsFollowers);

            // ScroolDown
            SchroolDownWaitLoop(Config.CssContactsListScrollable);
            HashSet<string> contactsFollowing = Selenium.GetAttributes(Config.UrlContacts)
                .ToHashSet();
            ClickWait(Config.CssContactsListClose);

            IEnumerable<string> result = Data.MyContacts
                .Except(contactsFollowing)
                .Except(MyContactsInTryout)
                .ToArray(); // solve
            int r = result.Count();
            if (r > 0)
            {
                if (Config.BotKeepSomeUnfollowerContacts > 0 && r > Config.BotKeepSomeUnfollowerContacts)
                {
                    r -= Config.BotKeepSomeUnfollowerContacts;
                }

                // Keep older first
                if (Data.ContactsToUnfollow.Any())    // all data will be retried, so clear cache if required
                {
                    Data.ContactsToUnfollow = new Queue<string>(
                        Data.ContactsToUnfollow
                            .Intersect(result)
                            .Take(r)
                        );
                    result = result.Except(Data.ContactsToUnfollow);
                    r -= Data.ContactsToUnfollow.Count;
                }
                // fill then
                foreach (string needToUnfollow in result
                    .Take(r))
                {
                    Data.ContactsToUnfollow.Enqueue(needToUnfollow);
                }
            }
            else if (Data.ContactsToUnfollow.Any())
            {
                Data.ContactsToUnfollow.Clear();
            }
            Log.LogDebug("$ContactsToUnfollow ={0}", Data.ContactsToUnfollow.Count);
        }

        private void SearchKeywords()
        {
            IEnumerable<string> keywords = Config.BotSearchKeywords?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>();
            foreach (string keyword in keywords)
            {
                Log.LogDebug("Searching {0}", keyword);

                MoveToWait(string.Format(Config.UrlSearch, HttpUtility.UrlEncode(keyword)));

                SchroolDownWaitLoop(Config.BotSearchScrools);

                string[] list = Selenium.GetAttributes(Config.CssExplorePhotos, "href", false, true)
                    .ToArray();// solve
                if (list.Any())
                {
                    int c = Data.PhotosToLike.Count;
                    foreach (string url in list
                        .Except(Data.PhotosToLike))
                    {
                        Data.PhotosToLike.Enqueue(url);
                    }
                    Log.LogDebug("$PhotosToLike +{0}", Data.PhotosToLike.Count - c);
                }
                else
                {
                    Log.LogWarning("Searching \"{0}\" with CSS \"{1}\" return nothing ! A CSS selector update may be required.", keyword, Config.CssExplorePhotos);
                }
            }
        }

        private void DoExplorePhotosPageActions(bool doLike, bool doFollow)
        {
            Log.LogDebug("Go to Explore page");
            ScrollToTopWait();
            ClickWait(Config.CssHeaderButtonExplore);

            SchroolDownWaitLoop(Config.BotExplorePhotosPageInitScrools);

            int likeTodo = doLike ? PseudoRand(Config.BotExplorePhotosPageLikeMin, Config.BotExplorePhotosPageLikeMax) : 0;
            int followTodo = doFollow ? PseudoRand(Config.BotExplorePhotosPageFollowMin, Config.BotExplorePhotosPageFollowMax) : 0;
            IWebElement element = Selenium.GetElements(Config.CssExplorePhotos, true, true).FirstOrDefault();

            int likeDone = 0;
            int followDone = 0;
            while (element != null && (likeDone < likeTodo || followDone < followTodo))
            {
                Log.LogDebug("Opening a post");
                ScrollClickWait(element);

                if (likeDone < likeTodo && Selenium.GetIfPresent(Config.CssPhotoLike, out IWebElement btnLike))
                {
                    Log.LogDebug("Liking");
                    WaitBeforeLikeHumanizer();
                    ClickWait(btnLike);
                    CheckActionWarning();
                    likeDone++;
                }

                if (followDone < followTodo && Selenium.GetIfPresent(Config.CssPhotoFollow, out IWebElement btnFollow))
                {
                    Log.LogDebug("Following");
                    WaitBeforeFollowHumanizer();
                    ClickWait(btnFollow);
                    CheckActionWarning();
                    followDone++;
                }

                // close modal page without waiter
                Log.LogDebug("Closing a post");
                Selenium.Click(Config.CssPhotoClose);

                // add more result in the page for next
                ScrollToBottomWait();

                element = Selenium.GetElements(Config.CssExplorePhotos, true, true).FirstOrDefault();
            }
            if (likeDone == likeTodo && followDone == followTodo)
            {
                Log.LogInformation("Explore Photos page actions : {0} like, {1} follow", likeDone, followDone);
            }
            else
            {
                Log.LogWarning("Explore Photos page actions : {0}/{1} like, {2}/{3} follow (not all done, you may increase scrool or reduce frequency)", likeDone, likeTodo, followDone, followTodo);
            }
        }

        private void DoActivityPageActions()
        {
            Log.LogDebug("Go to Activity page");
            ScrollToTopWait();
            ClickWait(Config.CssHeaderButtonActivity);

            WaitHumanizer();

            ClickByPositionWait(Config.CssHeaderButtonActivity);
        }

        private void DoHomePageActions()
        {
            Log.LogDebug("Go to Home page");
            ScrollToTopWait();
            ClickWait(Config.CssHeaderButtonHome);

            SchroolDownWaitLoop(Config.BotHomePageInitScrools);

            int likeTodo = PseudoRand(Config.BotHomePageLikeMin, Config.BotHomePageLikeMax);
            IWebElement element = Selenium.GetElements(Config.CssPhotoLike, true, true).FirstOrDefault();

            int likeDone = 0;
            while (element != null && likeDone < likeTodo)
            {
                Log.LogDebug("Liking");
                WaitBeforeLikeHumanizer();
                ScrollClickWait(element);
                CheckActionWarning();
                likeDone++;

                // add more result in the page for next
                ScrollToBottomWait();

                element = Selenium.GetElements(Config.CssPhotoLike, true, true).FirstOrDefault();
            }
            if (likeDone == likeTodo)
            {
                Log.LogInformation("Home page actions : {0} like", likeDone);
            }
            else
            {
                Log.LogWarning("Home page actions : {0}/{1} like (not all done, you may increase scrool or reduce frequency)", likeDone, likeTodo);
            }
        }

        private void DoContactsFollow()
        {
            int todo = PseudoRand(Config.BotFollowTaskBatchMinLimit, Config.BotFollowTaskBatchMaxLimit);
            int c = Data.ContactsToFollow.Count;
            while (Data.ContactsToFollow.TryDequeue(out string uri) && todo > 0)
            {
                MoveToWait(uri);
                MyContactsInTryout.Add(uri);
                if (Selenium.GetElements(Config.CssContactFollow).Any()) // manage the already followed like this
                {
                    WaitBeforeFollowHumanizer();
                    ClickWait(Config.CssContactFollow);
                    Data.MyContacts.Add(uri); // the url relad may break a waiting ball

                    // issue detection : too many actions lately ? should stop for 24-48h...
                    Selenium.CrashIfPresent(Config.CssActionWarning, "This action was blocked. Please try again later");

                    todo--;
                }
                else
                {
                    Data.MyContactsBanned.Add(uri); // avoid going back each time to a "requested" account
                }
            }
            Log.LogDebug("$ContactsToFollow -{0}", c - Data.ContactsToFollow.Count);
        }

        private void DoContactsUnfollow()
        {
            int todo = RandomNumberGenerator.GetInt32(Config.BotUnfollowTaskBatchMinLimit, Config.BotUnfollowTaskBatchMaxLimit);
            int c = Data.ContactsToUnfollow.Count;
            while (Data.ContactsToUnfollow.TryDequeue(out string uri) && todo > 0)
            {
                MoveToWait(uri);

                bool process = false;
                // with triangle
                if (Selenium.GetElements(Config.CssContactUnfollowButton).Any()) // manage the already unfollowed like this
                {
                    WaitBeforeFollowHumanizer();
                    Selenium.Click(Config.CssContactUnfollowButton);
                    process = true;
                }
                // without triangle
                else if (Selenium.GetElements(Config.CssContactUnfollowButtonAlt).Any()) // manage the already unfollowed like this
                {
                    WaitBeforeFollowHumanizer();
                    Selenium.Click(Config.CssContactUnfollowButtonAlt);
                    process = true;
                }
                if (process)
                {
                    WaitHumanizer();

                    ClickWait(Config.CssContactUnfollowConfirm);
                    CheckActionWarning();

                    Data.MyContacts.Remove(uri);
                    MyContactsInTryout.Remove(uri);
                    Data.MyContactsBanned.Add(uri);
                    todo--;
                }
            }
            Log.LogDebug("$ContactsToUnfollow -{0}", c - Data.ContactsToUnfollow.Count);
        }

        private void DoPhotosLike(bool doFollow = true, bool doLike = true)
        {
            int todo = PseudoRand(Config.BotLikeTaskBatchMinLimit, Config.BotLikeTaskBatchMaxLimit);
            int c = Data.PhotosToLike.Count;
            while (Data.PhotosToLike.TryDequeue(out string uri) && todo > 0)
            {
                MoveToWait(uri);

                if (doLike && Selenium.GetElements(Config.CssPhotoLike).Any()) // manage the already unfollowed like this
                {
                    WaitBeforeLikeHumanizer();
                    ClickWait(Config.CssPhotoLike);
                    CheckActionWarning();
                }

                if (doFollow && Selenium.GetElements(Config.CssPhotoFollow).Any()) // manage the already unfollowed like this
                {
                    WaitBeforeFollowHumanizer();
                    ClickWait(Config.CssPhotoFollow);
                    CheckActionWarning();
                }

                todo--;
            }
            Log.LogDebug("$PhotosToLike -{0}", c - Data.PhotosToLike.Count);
        }
    }
}