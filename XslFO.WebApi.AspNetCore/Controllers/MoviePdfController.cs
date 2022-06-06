﻿/*
Copyright 2012 Brandon Bernard

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.CustomExtensions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PdfTemplating.AspNetCoreMvc.Reports.PdfRenderers;
using XslFO.Samples.MovieSearchService;

namespace PdfTemplating.AspNetCoreMvc.Controllers
{
    [ApiController]
    [Route("movies/pdf")]
    public class MoviePdfController : Controller
    {
        [HttpGet]
        public async Task<ActionResult> Index(String title = "Star Wars", bool useRazor = true)
        {
            if (useRazor)
                return await PdfWithRazorAndApacheFOP(title);
            else
                return await PdfWithXsltAndApacheFOP(title);
        }

        [HttpGet]
        [Route("razor/apache-fop")]
        public async Task<ActionResult> PdfWithRazorAndApacheFOP(String title = "Star Wars")
        {
            try
            {
                var searchResponse = await ExecuteMovieSearchHelperAsync(title).ConfigureAwait(false);

                //*******************************************
                // RAZOR + Apace FOP (async I/O request)
                //*******************************************
                var pdfRenderer = new RazorMoviePdfRenderer(this);
                var pdfBytes = await pdfRenderer.RenderPdfAsync(searchResponse).ConfigureAwait(false);

                //Create the File Content Result from the Pdf byte data
                return new FileContentResult(pdfBytes, WebContentType.Pdf);
            }
            catch (Exception exc)
            {
                //Bubble up the Error as Json for additional Details
                return CreateJsonExceptionResult(exc);
            }
        }

        [HttpGet]
        [Route("xslt/apache-fop")]
        public async Task<ActionResult> PdfWithXsltAndApacheFOP(String title = "Star Wars")
        {
            try
            {
                var searchResponse = await ExecuteMovieSearchHelperAsync(title);

                //*******************************************
                // XSLT + Apache FOP (async I/O request)
                //*******************************************
                var pdfRenderer = new XsltMoviePdfRenderer();
                var pdfBytes = await pdfRenderer.RenderPdfAsync(searchResponse).ConfigureAwait(false);

                //Create the File Content Result from the Pdf byte data
                return new FileContentResult(pdfBytes, WebContentType.Pdf);
            }
            catch (Exception exc)
            {
                //Bubble up the Error as Json for additional Details
                return CreateJsonExceptionResult(exc);
            }
        }

        #region Helpers

        private async Task<MovieSearchResponse> ExecuteMovieSearchHelperAsync(String title)
        {
            //Execute the Move Search to load data . . . 
            //NOTE: This can come from any source, and can be from converted JSON or Xml, etc.
            //NOTE: As an interesting use case here, we load the results dynamically from a Movie Search
            //      Database REST call for JSON results, and convert to Xml dynamically to use efficiently
            //      with our templates.
            var movieSearchService = new MovieSearchService();
            var searchResponse = await movieSearchService.SearchAsync(title).ConfigureAwait(false);
            return searchResponse;
        }

        private ContentResult CreateJsonExceptionResult(Exception exc)
        {
            var exceptionJson = exc.ToJson();
            var resultContent = Content(exceptionJson, WebContentType.Json);
            return resultContent;
        }

        #endregion
    }


}