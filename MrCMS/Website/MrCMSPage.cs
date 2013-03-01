﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.Routing;
using MrCMS.Entities;
using MrCMS.Entities.Documents.Web;
using MrCMS.Services;
using MrCMS.Settings;
using MrCMS.Helpers;
using MrCMS.Website.Optimization;

namespace MrCMS.Website
{
    public abstract class MrCMSPage<TModel> : System.Web.Mvc.WebViewPage<TModel>
    {
        private IConfigurationProvider _configurationProvider;
        private IResourceBundler _resourceBundler;

        public T SiteSettings<T>() where T : SiteSettingsBase, new()
        {
            return _configurationProvider.GetSiteSettings<T>();
        }

        public T GlobalSettings<T>() where T : GlobalSettingsBase, new()
        {
            return _configurationProvider.GetGlobalSettings<T>();
        }

        public override void InitHelpers()
        {
            base.InitHelpers();

            if (CurrentRequestData.DatabaseIsInstalled)
            {
                _configurationProvider = MrCMSApplication.Get<IConfigurationProvider>();
                _resourceBundler = MrCMSApplication.Get<IResourceBundler>();
            }
        }

        public MvcHtmlString Editable<T>(T model, Expression<Func<T, string>> method, bool isHtml = false) where T : SystemEntity
        {
            if (model == null)
                return MvcHtmlString.Empty;

            var propertyInfo = PropertyFinder.GetProperty(method);
            var value = Html.ParseShortcodes(method.Compile().Invoke(model)).ToHtmlString();
            var typeName = typeof(T).Name;

            if (EditingEnabled  && propertyInfo != null)
            {
                var tagBuilder = new TagBuilder("div");
                tagBuilder.AddCssClass("editable");
                tagBuilder.Attributes["data-id"] = model.Id.ToString();
                tagBuilder.Attributes["data-property"] = propertyInfo.Name;
                tagBuilder.Attributes["data-type"] = typeName;
                tagBuilder.Attributes["data-is-html"] = isHtml ? "true" : "false";
                tagBuilder.InnerHtml = value;

                return MvcHtmlString.Create(tagBuilder.ToString());
            }
            return MvcHtmlString.Create(value);
        }

        private bool EditingEnabled
        {
            get { return CurrentRequestData.CurrentUserIsAdmin && _configurationProvider.GetSiteSettings<SiteSettings>().EnableInlineEditing; }
        }

        public MvcHtmlString RenderZone(string areaName)
        {
            var page = Model as Webpage;

            if (page != null && page.CurrentLayout != null)
            {
                var layout = page.CurrentLayout;

                var layoutArea = layout.GetLayoutAreas().FirstOrDefault(area => area.AreaName == areaName);

                if (layoutArea == null) return MvcHtmlString.Empty;

                var stringBuilder = new StringBuilder();
                if (EditingEnabled)
                    stringBuilder.AppendFormat("<div data-layout-area-id=\"{0}\" data-layout-area-name=\"{1}\" class=\"layout-area\"> ", layoutArea.Id, layoutArea.AreaName);

                foreach (var widget in layoutArea.GetWidgets(page))
                {
                    if (EditingEnabled)
                        stringBuilder.AppendFormat("<div data-widget-id=\"{0}\" class=\"widget\"> ", widget.Id);

                    try
                    {
                        stringBuilder.Append(Html.Action("Show", "Widget", new { widget }).ToHtmlString());
                    }
                    catch (Exception ex)
                    {
                        CurrentRequestData.ErrorSignal.Raise(ex);
                    }

                    if (EditingEnabled)
                        stringBuilder.Append("</div>");
                }

                if (EditingEnabled)
                    stringBuilder.Append("</div>");

                return MvcHtmlString.Create(stringBuilder.ToString());
            }
            return MvcHtmlString.Empty;
        }

        public MvcHtmlString RenderImage(string imageUrl, string alt = null, string title = null, object attributes = null)
        {
            return Html.RenderImage(imageUrl, alt, title, attributes);
        }

        public MvcHtmlString RenderImage(string imageUrl, Size size, string alt = null, string title = null, object attributes = null)
        {
            return Html.RenderImage(imageUrl, size, alt, title, attributes);
        }
    }

    public abstract class MrCMSPage : MrCMSPage<dynamic>
    {
    }

    public class PropertyFinder
    {
        public static PropertyInfo GetProperty(Expression expression)
        {
            return expression is LambdaExpression && (expression as LambdaExpression).Body is MemberExpression &&
                   ((expression as LambdaExpression).Body as MemberExpression).Member is PropertyInfo
                       ? ((expression as LambdaExpression).Body as MemberExpression).Member as PropertyInfo
                       : null;
        }
    }
}