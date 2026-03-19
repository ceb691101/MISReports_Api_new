using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/reportcategory")]
    public class ReportCategoryController : ApiController
    {
        private readonly ReportCategoryRepository _repository = new ReportCategoryRepository();

        /// <summary>
        /// Get all report categories
        /// </summary>
        /// <returns>List of all report categories</returns>
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAllCategories()
        {
            try
            {
                var result = _repository.GetAllCategories();

                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = result,
                    errorMessage = (string)null
                })));
            }
            catch (Exception ex)
            {
                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get report categories.",
                    errorDetails = ex.Message
                })));
            }
        }

        /// <summary>
        /// Get a specific report category by code
        /// </summary>
        /// <param name="catCode">The category code</param>
        /// <returns>Report category details</returns>
        [HttpGet]
        [Route("{catCode}")]
        public IHttpActionResult GetCategoryByCode(string catCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(catCode))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Category code is required."
                    }));
                }

                var result = _repository.GetCategoryByCode(catCode.Trim());

                if (result == null)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Category not found."
                    }));
                }

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get report category.",
                    errorDetails = ex.Message
                }));
            }
        }

        /// <summary>
        /// Create or update a report category
        /// </summary>
        /// <param name="request">CreateReportCategoryRequest object</param>
        /// <returns>Success/failure message</returns>
        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateCategory([FromBody] CreateReportCategoryRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Request body is required."
                    }));
                }

                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.CatCode))
                    validationErrors.Add("Category code is required.");

                if (string.IsNullOrWhiteSpace(request.CatName))
                    validationErrors.Add("Category name is required.");

                if (validationErrors.Count > 0)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = string.Join(" ", validationErrors)
                    }));
                }

                var created = _repository.AddOrUpdateCategory(request);

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        success = created,
                        catCode = request.CatCode?.Trim(),
                        message = "Category created/updated successfully."
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot create/update category.",
                    errorDetails = ex.Message
                }));
            }
        }

        /// <summary>
        /// Update a report category
        /// </summary>
        /// <param name="catCode">The category code to update</param>
        /// <param name="request">CreateReportCategoryRequest object</param>
        /// <returns>Success/failure message</returns>
        [HttpPut]
        [Route("{catCode}")]
        public IHttpActionResult UpdateCategory(string catCode, [FromBody] CreateReportCategoryRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Request body is required."
                    }));
                }

                if (string.IsNullOrWhiteSpace(catCode))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Category code is required."
                    }));
                }

                // Set the category code from the URL if not provided in the request
                if (string.IsNullOrWhiteSpace(request.CatCode))
                {
                    request.CatCode = catCode;
                }

                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.CatCode))
                    validationErrors.Add("Category code is required.");

                if (string.IsNullOrWhiteSpace(request.CatName))
                    validationErrors.Add("Category name is required.");

                if (validationErrors.Count > 0)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = string.Join(" ", validationErrors)
                    }));
                }

                var updated = _repository.UpdateCategory(request);

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        success = updated,
                        catCode = request.CatCode?.Trim(),
                        message = updated ? "Category updated successfully." : "Category not found."
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot update category.",
                    errorDetails = ex.Message
                }));
            }
        }

        /// <summary>
        /// Delete a report category
        /// </summary>
        /// <param name="catCode">The category code to delete</param>
        /// <returns>Success/failure message</returns>
        [HttpDelete]
        [Route("{catCode}")]
        public IHttpActionResult DeleteCategory(string catCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(catCode))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Category code is required."
                    }));
                }

                var deleted = _repository.DeleteCategory(catCode.Trim());

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        success = deleted,
                        catCode = catCode.Trim(),
                        message = deleted ? "Category deleted successfully." : "Category not found."
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot delete category.",
                    errorDetails = ex.Message
                }));
            }
        }
    }
}








