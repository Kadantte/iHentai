﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iHentai.Html.Attributes;
using iHentai.Services.Core;

namespace iHentai.Services.Manhuagui.Model
{
    class ManhuaguiGalleryList
    {
        [HtmlMultiItems("#detail > li")]
        public List<ManhuaguiGallery> List { get; set; }
    }

    class ManhuaguiGallery : IMangaGallery
    {
        [HtmlItem("h3")]
        public string Title { get; set; }
        
        [HtmlItem(".thumb > img", Attr = "data-src")]
        public string Thumb { get; set; }

        [HtmlItem("a", Attr = "href")]
        public string Link { get; set; }

        [HtmlItem("dl:last-child > :last-child")]
        public string UpdateAt { get; set; }
     
        public string Chapter { get; set; }
    }

    class ManhuaguiGalleryDetail : IMangaDetail
    {
        [HtmlItem(".main-bar > h1")]
        public string Title { get; set; }

        [HtmlItem(".thumb > img", Attr = "src")]
        public string Thumb { get; set; }

        [HtmlItem("#bookIntro")]
        public string Desc { get; set; }
        
        [HtmlMultiItems("#chapterList>ul>li")]
        public List<ManhuaguGalleryChapter> Chapters { get; set; }

        IEnumerable<IMangaChapter> IMangaDetail.Chapters => Chapters;
    }

    class ManhuaguGalleryChapter : IMangaChapter
    {
        [HtmlItem("a", Attr = "href")]
        public string Link { get; set; }

        [HtmlItem("a > b")]
        public string Title { get; set; }

        [HtmlItem(".new-icon")]
        [HtmlConverter(typeof(NullToBoolHtmlConverter))]
        public bool Updated { get; set; }
    }

}