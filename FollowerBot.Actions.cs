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

        private bool TryAuthCookies()
        {
            if (Data.Cookies != null && Data.Cookies.Any())
            {
                if (!MoveTo(Config.UrlRoot))
                {
                    throw new NotSupportedException("INSTAGRAM RETURN ERROR 500 ON " + Config.UrlRoot);
                }

                Selenium.Cookies = Data.Cookies; // need to have loaded the page 1st
                Selenium.SessionStorage = Data.SessionStorage; // need to have loaded the page 1st
                Selenium.LocalStorage = Data.LocalStorage; // need to have loaded the page 1st

                // Ignore the message bar : Allow Instagram Cookies
                ClickWaitIfPresent(Config.CssCookiesWarning);

                if (!MoveTo(Config.UrlRoot))
                {
                    throw new NotSupportedException("INSTAGRAM RETURN ERROR 500 ON " + Config.UrlRoot);
                }

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
            if (!MoveTo(Config.UrlLogin))
            {
                throw new NotSupportedException("INSTAGRAM RETURN ERROR ON " + Config.UrlLogin);
            }

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
                Selenium.Click(Config.CssLoginMyself); // must be here, else the auth have failed
                WaitHumanizer();
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
                MoveTo(Data.UserContactUrl);
                WaitHumanizer();

                Selenium.Click(Config.CssContactsFollowing);
                WaitHumanizer();

                // ScroolDown
                SchroolDownWaitLoop(Config.CssContactsListScrollable);   // TOFIX : will crash if no contact at all

                Data.MyContacts = Selenium.GetAttributes(Config.UrlContacts)
                    .ToHashSet();

                Log.LogDebug("MyContacts ={0}", Data.MyContacts.Count);

                Data.MyContactsUpdate = DateTime.UtcNow;
            }

            AddForced("AddContactsToFollow", Config.AddContactsToFollow, Data.ContactsToFollow);
            AddForced("AddPhotosToLike", Config.AddPhotosToLike, Data.PhotosToLike);
        }

        private void DetectContactsFollowBack()
        {
            MoveTo(Data.UserContactUrl);
            WaitHumanizer();

            Selenium.Click(Config.CssContactsFollowers);
            WaitHumanizer();

            SchroolDownWaitLoop(Config.CssContactsListScrollable);
            IEnumerable<string> list = Selenium.GetAttributes(Config.UrlContacts)
                                      .ToList(); // Solve

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

        private void ExplorePeopleSuggested()
        {
            MoveTo(Config.UrlExplorePeopleSuggested);
            WaitHumanizer();

            SchroolDownWaitLoop(Config.BotExplorePeopleSuggestedScrools);

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
            MoveTo(Config.UrlExplore);
            WaitHumanizer();

            SchroolDownWaitLoop(Config.BotExplorePhotosScrools);

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
            MoveTo(Data.UserContactUrl);
            WaitHumanizer();

            Selenium.Click(Config.CssContactsFollowers);
            WaitHumanizer();

            // ScroolDown
            SchroolDownWaitLoop(Config.CssContactsListScrollable);

            HashSet<string> contactsFollowing = Selenium.GetAttributes(Config.UrlContacts)
                .ToHashSet();

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

                MoveTo(string.Format(Config.UrlSearch, HttpUtility.UrlEncode(keyword)));
                WaitHumanizer();

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

        private void DoHomePhotosLike()
        {
            MoveTo(Config.UrlRoot);
            WaitHumanizer();

            SchroolDownWaitLoop(Config.BotHomeLikeInitScrools);

            int c = 0;
            int todo = PseudoRand.Next(Config.BotHomeLikeTaskBatchMinLimit, Config.BotHomeLikeTaskBatchMaxLimit);
            IWebElement element = Selenium.GetElements(Config.CssPhotoLike, true, true).FirstOrDefault();
            while (element != null && c < todo) // manage the already followed like this
            {
                Selenium.ScrollIntoView(element);
                WaitBeforeLikeHumanizer();
                element.Click();
                ScrollToBottomWait();
                WaitHumanizer();
                c++;

                // issue detection : too many actions lately ? should stop for 24-48h...
                Selenium.CrashIfPresent(Config.CssActionWarning, "This action was blocked. Please try again later");

                todo--;
                element = Selenium.GetElements(Config.CssPhotoLike, true, true).FirstOrDefault();
            }

            Log.LogDebug("Home Photos Liked {0}", c);
        }

        private void DoContactsFollow()
        {
            int todo = PseudoRand.Next(Config.BotFollowTaskBatchMinLimit, Config.BotFollowTaskBatchMaxLimit);
            int c = Data.ContactsToFollow.Count;
            while (Data.ContactsToFollow.TryDequeue(out string uri) && todo > 0)
            {
                if (!MoveTo(uri))
                {
                    Log.LogWarning("ACTION STOPED : INSTAGRAM RETURN ERROR ON ({0})", uri);
                    break; // no retry
                }
                MyContactsInTryout.Add(uri);
                if (Selenium.GetElements(Config.CssContactFollow).Any()) // manage the already followed like this
                {
                    WaitBeforeFollowHumanizer();
                    Selenium.Click(Config.CssContactFollow);
                    Data.MyContacts.Add(uri);
                    WaitHumanizer();// the url relad may break a waiting ball

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
                if (!MoveTo(uri))
                {
                    Log.LogWarning("ACTION STOPED : Instagram RETURN ERROR ({0})", uri);
                    break; // no retry
                }

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

                    Selenium.Click(Config.CssContactUnfollowConfirm);
                    WaitHumanizer();// the url relad may break a waiting ball

                    // issue detection : too many actions lately ? should stop for 24-48h...
                    Selenium.CrashIfPresent(Config.CssActionWarning, "This action was blocked. Please try again later");

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
            int todo = PseudoRand.Next(Config.BotLikeTaskBatchMinLimit, Config.BotLikeTaskBatchMaxLimit);
            int c = Data.PhotosToLike.Count;
            while (Data.PhotosToLike.TryDequeue(out string uri) && todo > 0)
            {
                if (!MoveTo(uri))
                {
                    Log.LogWarning("ACTION STOPED : Instagram RETURN ERROR ({0})", uri);
                    break; // no retry
                }

                if (doLike && Selenium.GetElements(Config.CssPhotoLike).Any()) // manage the already unfollowed like this
                {
                    WaitBeforeLikeHumanizer();
                    Selenium.Click(Config.CssPhotoLike);
                    WaitHumanizer();

                    // issue detection : too many actions lately ? should stop for 24-48h...
                    Selenium.CrashIfPresent(Config.CssActionWarning, "This action was blocked. Please try again later");
                }

                if (doFollow && Selenium.GetElements(Config.CssPhotoFollow).Any()) // manage the already unfollowed like this
                {
                    WaitBeforeFollowHumanizer();
                    Selenium.Click(Config.CssPhotoFollow);
                    WaitHumanizer();

                    // issue detection : too many actions lately ? should stop for 24-48h...
                    Selenium.CrashIfPresent(Config.CssActionWarning, "This action was blocked. Please try again later");
                }

                todo--;
            }
            Log.LogDebug("$PhotosToLike -{0}", c - Data.PhotosToLike.Count);
        }
    }
}