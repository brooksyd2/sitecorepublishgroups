using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Extensions;
using Sitecore.Globalization;
using Sitecore.Jobs;
using Sitecore.Publishing;
using Sitecore.Security.AccessControl;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sitecore.PublishTargetGroups
{
    public class PublishForm : WizardForm
    {
        /// <summary>
        /// The error text.
        /// 
        /// </summary> 
        /// 
        protected Memo ErrorText;
        /// <summary>
        /// The incremental publish.
        /// 
        /// </summary>
        protected Radiobutton IncrementalPublish;
        /// <summary>
        /// The incremental publish pane.
        /// 
        /// </summary>
        protected Border IncrementalPublishPane;
        /// <summary>
        /// The languages.
        /// 
        /// </summary>
        protected Border Languages;
        /// <summary>
        /// The languages panel.
        /// 
        /// </summary>
        protected Groupbox LanguagesPanel;
        /// <summary>
        /// The no publishing target.
        /// 
        /// </summary>
        protected Border NoPublishingTarget;
        /// <summary>
        /// The publish children.
        /// 
        /// </summary>
        protected Checkbox PublishChildren;
        /// <summary>
        /// The publish children pane.
        /// 
        /// </summary>
        protected Border PublishChildrenPane;
        /// <summary>
        /// The publishing panel.
        /// 
        /// </summary>
        protected Groupbox PublishingPanel;
        /// <summary>
        /// The publishing targets.
        /// 
        /// </summary>
        protected Border PublishingTargets;
        /// <summary>
        /// The publishing targets panel.
        /// 
        /// </summary>
        protected Groupbox PublishingTargetsPanel;
        /// <summary>
        /// The publishing target groups.
        /// 
        /// </summary>
        protected Border PublishingTargetGroups;
        /// <summary>
        /// The publishing target groups panel.
        /// 
        /// </summary>
        protected Groupbox PublishingTargetGroupsPanel;
        /// <summary>
        /// The publishing text.
        /// 
        /// </summary>
        protected Border PublishingText;
        /// <summary>
        /// The republish.
        /// 
        /// </summary>
        protected Radiobutton Republish;
        /// <summary>
        /// The result label.
        /// 
        /// </summary>
        protected Border ResultLabel;
        /// <summary>
        /// The result text.
        /// 
        /// </summary>
        protected Memo ResultText;
        /// <summary>
        /// The settings pane.
        /// 
        /// </summary>
        protected Scrollbox SettingsPane;
        /// <summary>
        /// The show result pane.
        /// 
        /// </summary>
        protected Border ShowResultPane;
        /// <summary>
        /// The smart publish.
        /// 
        /// </summary>
        protected Radiobutton SmartPublish;
        /// <summary>
        /// The status.
        /// 
        /// </summary>
        protected Literal Status;
        /// <summary>
        /// The welcome.
        /// 
        /// </summary>
        protected Literal Welcome;

        /// <summary>
        /// Gets or sets the item ID.
        /// 
        /// </summary>
        /// 
        /// <value>
        /// The item ID.
        /// </value>
        /// <contract><requires name="value" condition="not null"/><ensures condition="not null"/></contract>
        protected string ItemID
        {
            get
            {
                return StringUtil.GetString(this.ServerProperties["ItemID"]);
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                this.ServerProperties["ItemID"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the handle.
        /// 
        /// </summary>
        /// 
        /// <value>
        /// The handle.
        /// </value>
        /// <contract><requires name="value" condition="not empty"/><ensures condition="not null"/></contract>
        protected string JobHandle
        {
            get
            {
                return StringUtil.GetString(this.ServerProperties["JobHandle"]);
            }
            set
            {
                Assert.ArgumentNotNullOrEmpty(value, "value");
                this.ServerProperties["JobHandle"] = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Sitecore.Shell.Applications.Dialogs.Publish.PublishForm"/> is rebuild.
        /// 
        /// </summary>
        /// 
        /// <value>
        /// <c>true</c> if rebuild; otherwise, <c>false</c>.
        /// </value>
        protected bool Rebuild
        {
            get
            {
                return WebUtil.GetQueryString("mo") == "rebuild";
            }
        }

        /// <summary>
        /// Checks the status.
        /// 
        /// </summary>
        /// <exception cref="T:System.Exception"><c>Exception</c>.
        ///             </exception>
        public void CheckStatus()
        {
            Handle handle = Handle.Parse(this.JobHandle);
            if (!handle.IsLocal)
            {
                SheerResponse.Timer("CheckStatus", Settings.Publishing.PublishDialogPollingInterval);
            }
            else
            {
                PublishStatus status = PublishManager.GetStatus(handle);
                if (status == null)
                    throw new Exception("The publishing process was unexpectedly interrupted.");
                if (status.Failed)
                {
                    this.Active = "Retry";
                    this.NextButton.Disabled = true;
                    this.BackButton.Disabled = false;
                    this.CancelButton.Disabled = false;
                    this.ErrorText.Value = StringUtil.StringCollectionToString(status.Messages);
                }
                else
                {
                    string publishingTargetHtml;
                    if (status.State == JobState.Running)
                        publishingTargetHtml = string.Format("{0} {1}<br/><br/>{2} {3}<br/><br/>{4} {5}",
                            Translate.Text("Database:"),
                            StringUtil.Capitalize(status.CurrentTarget.NullOr(db => db.Name)),
                            Translate.Text("Language:"),
                            status.CurrentLanguage.NullOr(lang => lang.CultureInfo.DisplayName),
                            Translate.Text("Processed:"),
                            status.Processed);
                    else
                        publishingTargetHtml = status.State != JobState.Initializing ? Translate.Text("Queued.") : Translate.Text("Initializing.");
                    if (status.IsDone)
                    {
                        this.Status.Text = Translate.Text("Items processed: {0}.", new object[] { status.Processed.ToString(CultureInfo.InvariantCulture) });
                        this.Active = "LastPage";
                        this.BackButton.Disabled = true;
                        string result = StringUtil.StringCollectionToString(status.Messages, "\n");
                        if (string.IsNullOrEmpty(result))
                            return;
                        this.ResultText.Value = result;
                    }
                    else
                    {
                        SheerResponse.SetInnerHtml("PublishingTarget", publishingTargetHtml);
                        SheerResponse.Timer("CheckStatus", Settings.Publishing.PublishDialogPollingInterval);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the active page has been changed.
        /// 
        /// </summary>
        /// <param name="page">The page that has been entered.
        ///             </param><param name="oldPage">The page that was left.
        ///             </param>
        protected override void ActivePageChanged(string page, string oldPage)
        {
            Assert.ArgumentNotNull(page, "page");
            Assert.ArgumentNotNull(oldPage, "oldPage");
            this.NextButton.Header = Translate.Text("Next >");
            if (page == "Settings")
                this.NextButton.Header = Translate.Text("Publish") + " >";
            base.ActivePageChanged(page, oldPage);
            if (page != "Publishing")
                return;
            this.NextButton.Disabled = true;
            this.BackButton.Disabled = true;
            this.CancelButton.Disabled = true;
            if (Context.ClientPage.ClientRequest.Form["PublishMode"] == "IncrementalPublish")
                SheerResponse.SetInnerHtml("PublishingText", Translate.Text("Publishing Incrementally..."));
            else
                SheerResponse.SetInnerHtml("PublishingText", Translate.Text("Publishing..."));
            SheerResponse.SetInnerHtml("PublishingTarget", "&nbsp;");
            SheerResponse.Timer("StartPublisher", 10);
        }

        /// <summary>
        /// Called when the active page is changing.
        /// 
        /// </summary>
        /// <param name="page">The page that is being left.
        ///             </param><param name="newpage">The new page that is being entered.
        ///             </param>
        /// <returns>
        /// True, if the change is allowed, otherwise false.
        /// 
        /// </returns>
        /// 
        /// <remarks>
        /// Set the newpage parameter to another page ID to control the
        ///             path through the wizard pages.
        /// 
        /// </remarks>
        protected override bool ActivePageChanging(string page, ref string newpage)
        {
            Assert.ArgumentNotNull(page, "page");
            Assert.ArgumentNotNull(newpage, "newpage");
            if (page == "Retry")
                newpage = "Settings";
            if (newpage == "Publishing")
            {
                if (GetLanguages().Length == 0)
                {
                    SheerResponse.Alert(Translate.Text("You must pick at least one language to publish."), new string[0]);
                    return false;
                }
                if (GetPublishingTargetDatabases().Length == 0)
                {
                    SheerResponse.Alert(Translate.Text("You must pick at least one publishing target group."), new string[0]);
                    return false;
                }
            }
            return base.ActivePageChanging(page, ref newpage);
        }

        protected bool HasRepublishAccess
        {
            get
            {
                var currentUser = Sitecore.Security.Authentication.AuthenticationManager.GetActiveUser();
                if (currentUser == null)
                    return true; // Rely on standard Sitecore behaviour
                if (currentUser.IsAdministrator)
                    return true; // Allways allow admins
                var role = Sitecore.Security.Domains.Domain.GetDomain("sitecore").GetRoles()
                    .FirstOrDefault(r => string.Compare(r.Name, @"sitecore\RePublish", StringComparison.InvariantCultureIgnoreCase) == 0);
                if (role == null)
                    return true;
                return currentUser.IsInRole(role);
            }
        }

        /// <summary>
        /// Raises the load event.
        /// 
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.
        ///             </param>
        /// <remarks>
        /// This method notifies the server control that it should perform actions common to each HTTP
        ///             request for the page it is associated with, such as setting up a database query. At this
        ///             stage in the page lifecycle, server controls in the hierarchy are created and initialized,
        ///             view state is restored, and form controls reflect client-side data. Use the IsPostBack
        ///             property to determine whether the page is being loaded in response to a client postback,
        ///             or if it is being loaded and accessed for the first time.
        /// 
        /// </remarks>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (Context.ClientPage.IsEvent)
                return;
            this.ItemID = WebUtil.GetQueryString("id");
            this.PublishingTargetsPanel.Visible = false;
            this.BuildPublishingTargetGroups();
            this.BuildLanguages();
            this.IncrementalPublish.Checked = Registry.GetBool("/Current_User/Publish/IncrementalPublish", true);
            this.SmartPublish.Checked = Registry.GetBool("/Current_User/Publish/SmartPublish", false);
            this.Republish.Checked = Registry.GetBool("/Current_User/Publish/Republish", false);
            if (!HasRepublishAccess)
            {
                this.Republish.Checked = false;
                this.Republish.Disabled = true;
            }

            this.PublishChildren.Checked = Registry.GetBool("/Current_User/Publish/PublishChildren", true);
            if (!string.IsNullOrEmpty(this.ItemID))
            {
                this.IncrementalPublishPane.Style["display"] = "none";
                this.Welcome.Text = Translate.Text("Welcome to the Publish Item Wizard.");
                if (!this.IncrementalPublish.Checked)
                    return;
                this.IncrementalPublish.Checked = false;
                this.SmartPublish.Checked = true;
            }
            else if (this.Rebuild)
            {
                this.PublishingPanel.Style["display"] = "none";
                this.LanguagesPanel.Style["display"] = "none";
                this.Welcome.Text = Translate.Text("Rebuild databases");
                this.IncrementalPublish.Checked = false;
                this.SmartPublish.Checked = false;
            }
            else
            {
                this.PublishChildrenPane.Visible = false;
                Assert.CanRunApplication("Publish");
            }
        }

        /// <summary>
        /// Shows the result.
        /// 
        /// </summary>
        protected void ShowResult()
        {
            this.ShowResultPane.Visible = false;
            this.ResultText.Visible = true;
            this.ResultLabel.Visible = true;
        }

        /// <summary>
        /// Starts the publisher.
        /// 
        /// </summary>
        protected void StartPublisher()
        {
            Language[] languages = GetLanguages();
            List<Item> publishingTargets = GetPublishingTargets();
            Database[] publishingTargetDatabases = GetPublishingTargetDatabases();
            bool flag1 = Context.ClientPage.ClientRequest.Form["PublishMode"] == "IncrementalPublish";
            bool flag2 = Context.ClientPage.ClientRequest.Form["PublishMode"] == "SmartPublish";
            bool flag3 = Context.ClientPage.ClientRequest.Form["PublishMode"] == "Republish";
            bool rebuild = this.Rebuild;
            bool @checked = this.PublishChildren.Checked;
            if (rebuild)
                Log.Audit(this, "Rebuild database, databases: {0}", new string[] { StringUtil.Join(publishingTargetDatabases, ", ") });
            else
                Log.Audit(this, "Publish, languages:{0}, targets:{1}, databases:{2}, incremental:{3}, smart:{4}, republish:{5}, children:{6}", StringUtil.Join(languages, ", "), StringUtil.Join(publishingTargets, ", ", "Name"), StringUtil.Join(publishingTargetDatabases, ", "), MainUtil.BoolToString(flag1), MainUtil.BoolToString(flag2), MainUtil.BoolToString(flag3), MainUtil.BoolToString(@checked));
            var listString1 = new ListString();
            foreach (Language language in languages)
                listString1.Add(language.ToString());
            Registry.SetString("/Current_User/Publish/Languages", listString1.ToString());
            var listString2 = new ListString();
            foreach (Item obj in publishingTargets)
                listString2.Add(obj.ID.ToString());
            Registry.SetString("/Current_User/Publish/Targets", listString2.ToString());
            Registry.SetBool("/Current_User/Publish/IncrementalPublish", flag1);
            Registry.SetBool("/Current_User/Publish/SmartPublish", flag2);
            Registry.SetBool("/Current_User/Publish/Republish", flag3);
            Registry.SetBool("/Current_User/Publish/PublishChildren", @checked);
            this.JobHandle = (string.IsNullOrEmpty(this.ItemID) ? (!flag1 ? (!flag2 ? (!rebuild ? PublishManager.Republish(Sitecore.Client.ContentDatabase, publishingTargetDatabases, languages, Context.Language) : PublishManager.RebuildDatabase(Sitecore.Client.ContentDatabase, publishingTargetDatabases)) : PublishManager.PublishSmart(Sitecore.Client.ContentDatabase, publishingTargetDatabases, languages, Context.Language)) : PublishManager.PublishIncremental(Sitecore.Client.ContentDatabase, publishingTargetDatabases, languages, Context.Language)) : PublishManager.PublishItem(Sitecore.Client.GetItemNotNull(this.ItemID), publishingTargetDatabases, languages, @checked, flag2)).ToString();
            SheerResponse.Timer("CheckStatus", Settings.Publishing.PublishDialogPollingInterval);
        }

        /// <summary>
        /// Gets the languages.
        /// 
        /// </summary>
        /// 
        /// <returns>
        /// The languages.
        /// 
        /// </returns>
        private static Language[] GetLanguages()
        {
            var arrayList = new ArrayList();
            foreach (string index in Context.ClientPage.ClientRequest.Form.Keys)
            {
                if (index != null && index.StartsWith("la_", StringComparison.InvariantCulture))
                    arrayList.Add(Language.Parse(Context.ClientPage.ClientRequest.Form[index]));
            }
            return arrayList.ToArray(typeof(Language)) as Language[];
        }

        /// <summary>
        /// Gets the publishing target databases.
        /// 
        /// </summary>
        /// 
        /// <returns>
        /// The publishing target databases.
        /// 
        /// </returns>
        private static Database[] GetPublishingTargetDatabases()
        {
            var arrayList = new ArrayList();
            foreach (Item item in GetPublishingTargets())
            {
                string name = item["Target database"];
                Database database = Factory.GetDatabase(name);
                Assert.IsNotNull(database, typeof(Database), Translate.Text("Database \"{0}\" not found."), new object[] { name });
                arrayList.Add(database);
            }
            return arrayList.ToArray(typeof(Database)) as Database[];
        }

        /// <summary>
        /// Gets the publishing targets.
        /// 
        /// </summary>
        /// 
        /// <returns>
        /// The publishing targets.
        /// 
        /// </returns>
        /// <contract><ensures condition="not null"/></contract>
        private static List<Item> GetPublishingTargets()
        {
            var list = new List<Item>();
            foreach (string str in Context.ClientPage.ClientRequest.Form.Keys)
            {
                if (str != null && str.StartsWith("pb_", StringComparison.InvariantCulture))
                {
                    Item obj = Context.ContentDatabase.Items[ShortID.Decode(str.Substring(3))];
                    Assert.IsNotNull(obj, typeof(Item), "Publishing group not found.", new object[0]);

                    var targetList = obj["Publishing Targets"];

                    if (targetList == null || targetList == "")
                    {
                        list.Add(obj);
                    }
                    else
                    {
                        var targets = targetList.Split('|');

                        foreach (var child in targets)
                        {
                            var targetItem = Context.ContentDatabase.GetItem(child);
                            list.Add(targetItem);
                        }
                    }

                }
            }
            return list;
        }

        private bool CanPublishLanguage(Item languageItem)
        {
            return AuthorizationManager.IsAllowed(languageItem, AccessRight.LanguageWrite, Context.User);
        }


        /// <summary>
        /// Builds the languages.
        /// 
        /// </summary>
        private void BuildLanguages()
        {
            this.Languages.Controls.Clear();
            HtmlGenericControl checkboxControl = null;
            var profileLanguages = new ListString(Registry.GetString("/Current_User/Publish/Languages"));
            var languages = (from l in LanguageManager.GetLanguages(Context.ContentDatabase) orderby l.CultureInfo.DisplayName select l).ToList<Language>();
            foreach (Language language in languages)
            {
                if (Settings.CheckSecurityOnLanguages)
                {
                    ID languageItemId = LanguageManager.GetLanguageItemId(language, Context.ContentDatabase);
                    if (!ItemUtil.IsNull(languageItemId))
                    {
                        Item languageItem = Context.ContentDatabase.GetItem(languageItemId);
                        if (languageItem == null || !CanPublishLanguage(languageItem))
                            continue;
                    }
                    else
                        continue;
                }
                string uniqueId = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("la_");
                checkboxControl = new HtmlGenericControl("input");
                this.Languages.Controls.Add(checkboxControl);
                checkboxControl.Attributes["type"] = "checkbox";
                checkboxControl.ID = uniqueId;
                checkboxControl.Attributes["value"] = language.Name;
                if (profileLanguages.IndexOf(language.ToString()) >= 0)
                    checkboxControl.Attributes["checked"] = "checked";
                var labelControl = new HtmlGenericControl("label");
                this.Languages.Controls.Add(labelControl);
                labelControl.Attributes["for"] = uniqueId;
                labelControl.InnerText = language.CultureInfo.DisplayName;
                this.Languages.Controls.Add(new LiteralControl("<br>"));
            }
            if (languages.Count != 1 || checkboxControl == null)
                return;
            this.LanguagesPanel.Disabled = true;
            checkboxControl.Attributes["checked"] = "checked";
            checkboxControl.Attributes["disabled"] = "disabled";
        }

        /// <summary>
        /// Builds the publishing target groups.
        /// 
        /// </summary>
        private void BuildPublishingTargetGroups()
        {
            this.PublishingTargetGroups.Controls.Clear();

            Item publishTargetsItem = Sitecore.Client.GetItemNotNull("/sitecore/system/publishing groups");
            string defaultPublishingTargets = Settings.GetSetting("DefaultPublishingTargetGroups").ToLowerInvariant();
            ChildList children = publishTargetsItem.Children;
            if (children.Count <= 0)
            {
                this.PublishingTargetGroupsPanel.Visible = false;
                this.PublishingTargetsPanel.Visible = true;
                BuildPublishingTargets();
            }
            else
            {
                HtmlGenericControl checkboxControl = null;
                foreach (Item item in children)
                {
                    string id = "pb_" + ShortID.Encode(item.ID);
                    checkboxControl = new HtmlGenericControl("input");
                    this.PublishingTargetGroups.Controls.Add(checkboxControl);
                    checkboxControl.Attributes["type"] = "checkbox";
                    checkboxControl.ID = id;
                    bool targetChecked = defaultPublishingTargets.IndexOf("|" + item.Key + "|", StringComparison.InvariantCulture) >= 0;

                    if (targetChecked)
                        checkboxControl.Attributes["checked"] = "checked";
                    checkboxControl.Disabled = children.Count == 1 || !item.Access.CanWrite();
                    var labelControl = new HtmlGenericControl("label");
                    this.PublishingTargetGroups.Controls.Add(labelControl);
                    labelControl.Attributes["for"] = id;
                    labelControl.InnerText = item.DisplayName;
                    this.PublishingTargetGroups.Controls.Add(new LiteralControl("<br>"));
                }
                if (children.Count != 1)
                    return;
                this.PublishingTargetGroupsPanel.Disabled = true;
                if (checkboxControl == null)
                    return;
                checkboxControl.Attributes["checked"] = "checked";
            }
        }


        /// <summary>
        /// Builds the publishing targets.
        /// 
        /// </summary>
        private void BuildPublishingTargets()
        {
            this.PublishingTargets.Controls.Clear();
            Item itemNotNull = Sitecore.Client.GetItemNotNull("/sitecore/system/publishing targets");
            ListString listString = new ListString(Registry.GetString("/Current_User/Publish/Targets"));
            string str1 = Settings.DefaultPublishingTargets.ToLowerInvariant();
            ChildList children = itemNotNull.Children;
            if (children.Count <= 0)
            {
                this.SettingsPane.Visible = false;
                this.NoPublishingTarget.Visible = true;
                this.NextButton.Disabled = true;
            }
            else
            {
                HtmlGenericControl htmlGenericControl1 = (HtmlGenericControl)null;
                foreach (Item obj in children)
                {
                    string str2 = "pb_" + ShortID.Encode(obj.ID);
                    htmlGenericControl1 = new HtmlGenericControl("input");
                    this.PublishingTargets.Controls.Add((System.Web.UI.Control)htmlGenericControl1);
                    htmlGenericControl1.Attributes["type"] = "checkbox";
                    htmlGenericControl1.ID = str2;
                    bool flag = false;// str1.IndexOf((string)(object)'|' + (object)obj.Key + (string)(object)'|', StringComparison.InvariantCulture) >= 0;
                    if (listString.Contains(obj.ID.ToString()))
                        flag = true;
                    if (flag)
                        htmlGenericControl1.Attributes["checked"] = "checked";
                    htmlGenericControl1.Disabled = children.Count == 1 || !obj.Access.CanWrite();
                    HtmlGenericControl htmlGenericControl2 = new HtmlGenericControl("label");
                    this.PublishingTargets.Controls.Add((System.Web.UI.Control)htmlGenericControl2);
                    htmlGenericControl2.Attributes["for"] = str2;
                    htmlGenericControl2.InnerText = obj.DisplayName;
                    this.PublishingTargets.Controls.Add((System.Web.UI.Control)new LiteralControl("<br>"));
                }
                if (children.Count != 1)
                    return;
                this.PublishingTargetsPanel.Disabled = true;
                if (htmlGenericControl1 == null)
                    return;
                htmlGenericControl1.Attributes["checked"] = "checked";
            }
        }
    }
}