using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Services;
using Umbraco.Core.Models;

namespace UmbracoDeployIssue.Code
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class ApplicationComposer : ComponentComposer<ApplicationComponent>, IUserComposer
    {
    }

    public class ApplicationComponent : IComponent
    {
        public void Initialize()
        {
            var factory = Umbraco.Web.Composing.Current.Factory;
            var contentTypeService = factory.GetInstance<IContentTypeService>();
            var dataTypeService = factory.GetInstance<IDataTypeService>();

            var basePage = new
            {
                Name = "Base Page",
                Alias = "BasePage"
            };
            
            var basePageContentType = contentTypeService.Get(basePage.Alias) ?? new ContentType(-1);
            basePageContentType.Name = basePage.Name;
            basePageContentType.Alias = basePage.Alias;
            basePageContentType.ParentId = -1;
            basePageContentType.Icon = "icon-document";
            basePageContentType.Description = "I am a base page.";
            basePageContentType.AllowedAsRoot = false;
            basePageContentType.Variations = ContentVariation.Culture;
            basePageContentType.IsElement = false;
            basePageContentType.IsContainer = false;
            
            if (basePageContentType.HasIdentity == false)
            {
                contentTypeService.Save(basePageContentType);
            }
            //else
            //{
            //    contentTypeService.Save(basePageContentType);
            //}
            
            var textBoxDataType = dataTypeService.GetAll(Constants.DataTypes.Textbox).First();

            CreateSinglePropertyWithGroup(basePageContentType, textBoxDataType, "SomeProperty", "SomenTabName", "SomeTabParent/SomeTabChild", parentTabName: "SomeTabParent");
            
            var startPage = new
            {
                Name = "Start Page",
                Alias = "StartPage"
            };

            var startPageContentType = contentTypeService.Get(startPage.Alias) ?? new ContentType(basePageContentType.Id);
            startPageContentType.Name = startPage.Name;
            startPageContentType.Alias = startPage.Alias;
            startPageContentType.ParentId = basePageContentType.Id;
            startPageContentType.Icon = "icon-document";
            startPageContentType.Description = "I am a start page.";
            startPageContentType.AllowedAsRoot = true;
            startPageContentType.Variations = ContentVariation.Culture;
            startPageContentType.IsElement = false;
            startPageContentType.IsContainer = false;
            startPageContentType.AddContentType(basePageContentType);

            if (startPageContentType.HasIdentity == false)
            {
                contentTypeService.Save(startPageContentType);
            }
            //else
            //{
            //    contentTypeService.Save(startPageContentType);
            //}

            CreateSinglePropertyWithGroup(startPageContentType, textBoxDataType, "StartPageProperty1", "StartPageTabName", "StartPageTabName", PropertyGroupType.Tab);
            CreateSinglePropertyWithGroup(startPageContentType, textBoxDataType, "StartPageProperty2", "StartPageGroupTransformedToTabName", "StartPageGroupTransformedToTabName", PropertyGroupType.Group);

            // Change existing group to a tab
            CreateSinglePropertyWithGroup(startPageContentType, textBoxDataType, "StartPageProperty2", "StartPageGroupTransformedToTabName", "StartPageGroupTransformedToTabName", PropertyGroupType.Tab);


            var articlePage = new
            {
                Name = "Article Page",
                Alias = "ArticlePage"
            };

            var articlePageContentType = contentTypeService.Get(articlePage.Alias) ?? new ContentType(basePageContentType.Id);
            articlePageContentType.Name = articlePage.Name;
            articlePageContentType.Alias = articlePage.Name;
            articlePageContentType.ParentId = basePageContentType.Id;
            articlePageContentType.Icon = "icon-document";
            articlePageContentType.Description = "I am a article page.";
            articlePageContentType.AllowedAsRoot = true;
            articlePageContentType.Variations = ContentVariation.Culture;
            articlePageContentType.IsElement = false;
            articlePageContentType.IsContainer = false;
            articlePageContentType.AddContentType(basePageContentType);
            
            if (articlePageContentType.HasIdentity == false)
            {
                contentTypeService.Save(articlePageContentType);
            }
            //else
            //{
            //    contentTypeService.Save(articlePageContentType);
            //}

            CreateSinglePropertyWithGroup(articlePageContentType, textBoxDataType, "ArticlePageProperty1", "ArticlePageTabName", "ArticlePageTabName", PropertyGroupType.Tab);
        }

        public void CreateSinglePropertyWithGroup(IContentType contentType, IDataType dataType, string propertyName, string propertyGroupName, string propertyGroupAlias, PropertyGroupType propertyGroupType = PropertyGroupType.Group, string parentTabName = null)
        {
            var factory = Umbraco.Web.Composing.Current.Factory;
            var contentTypeService = factory.GetInstance<IContentTypeService>();
            var propertyGroupIsChangedHack = false;
            
            // Add the property group
            var propertyGroupIndex = contentType.PropertyGroups.IndexOfKey(propertyGroupAlias);

            if (propertyGroupIndex == -1)
            {
                propertyGroupIndex = contentType.PropertyGroups.IndexOfKey(propertyGroupName);
                if (propertyGroupIndex == -1)
                {
                    contentType.AddPropertyGroup(propertyGroupAlias, propertyGroupName);
                    propertyGroupIndex = contentType.PropertyGroups.IndexOfKey(propertyGroupAlias);
                }
            }

            var propertyGroup = contentType.PropertyGroups[propertyGroupIndex];

            propertyGroup.Type = propertyGroupType;
            propertyGroup.Name = propertyGroupName;

            if (propertyGroupAlias.Contains("/") && string.IsNullOrEmpty(parentTabName) == false)
            {
                parentTabName = parentTabName.ToSafeAlias(true);
                var parentTabAlias = propertyGroupAlias.Split('/').First();
                contentType.AddPropertyGroup(parentTabAlias, parentTabName);

                var parentTabIndex = contentType.PropertyGroups.IndexOfKey(parentTabAlias);
                var parentTab = contentType.PropertyGroups[parentTabIndex];

                parentTab.Name = parentTabName;
                parentTab.Type = PropertyGroupType.Tab;

                propertyGroupIsChangedHack = parentTab.IsDirty();
                propertyGroup.UpdateParentAlias(parentTabAlias);
            }

            // Ensure propertyGroup order
            var groupSortOrder = 13; // some logic to calcualte sort order
            if (propertyGroup.SortOrder != groupSortOrder)
            {
                propertyGroup.SortOrder = groupSortOrder;
                contentTypeService.Save(contentType);
                contentType = contentTypeService.Get(contentType.Id); // Refresh: No idea if this is needed before in version 7 we had to do this.
            }

            if (propertyGroupIsChangedHack || propertyGroup.IsDirty())
            {
                contentTypeService.Save(contentType);
                contentType = contentTypeService.Get(contentType.Id); // Refresh: No idea if this is needed before in version 7 we had to do this.
            }

            var propertyToLookfor = propertyName;
            PropertyType umbracoProperty;
            var propertyExists = contentType.PropertyTypeExists(propertyToLookfor);
            if (propertyExists)
            {
                umbracoProperty = contentType.PropertyTypes.First(x => x.Alias.Equals(propertyToLookfor));
                contentType.MovePropertyType(umbracoProperty.Alias, propertyGroupName);

                contentTypeService.Save(contentType);
                contentType = contentTypeService.Get(contentType.Id); // Refresh: No idea if this is needed before in version 7 we had to do this.
            }
            else
            {
                umbracoProperty = new PropertyType(dataType);
                umbracoProperty.Name = propertyToLookfor;
                umbracoProperty.Alias = propertyToLookfor;
                umbracoProperty.Variations = ContentVariation.Culture;
                umbracoProperty.Description = "I am some property.";
                umbracoProperty.SortOrder = 1000;
                contentType.AddPropertyType(umbracoProperty, propertyGroupAlias, propertyGroupName);

                contentTypeService.Save(contentType);
                contentType = contentTypeService.Get(contentType.Id); // Refresh: No idea if this is needed before in version 7 we had to do this.
            }
        }

        public void Terminate()
        {
        }
    }
}