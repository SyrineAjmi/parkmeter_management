using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using parkmeter_management.Api.DTOs;
using parkmeter_management.Api.Mappers;
using parkmeter_management.Api.Requests.Site;
using parkmeter_management.Application.Managers.Interfaces;

namespace parkmeter_management.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SitesController : ControllerBase
    {
        private readonly ISiteManager _siteManager;

        public SitesController(ISiteManager siteManager)
        {
            _siteManager = siteManager;
        }

        //----------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sites = await _siteManager.GetAllSites();
            return Ok(sites.ToDtos());

        }

        //----------------------------------------------------------

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var site = await _siteManager.GetById(id);
            if (site == null) return NotFound();
            return Ok(site.ToDto());

        }

        //----------------------------------------------------------

        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateSiteRequest request)
        {
            var id = await _siteManager.CreateSite(
                request.Name,
                request.Address,
                request.Description);
            return Ok(new { Id = id, Message = "Site created successfully" });
        }

        //----------------------------------------------------------

        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            Guid id, [FromBody] UpdateSiteRequest request)
        {
            var result = await _siteManager.UpdateSite(
                id, request.Name, request.Address, request.Description);
            if (!result) return NotFound();
            return Ok("Site updated successfully");
        }

        //----------------------------------------------------------

        [Authorize(Roles = "GESTIONNAIRE_SYSTEME")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _siteManager.DeleteSite(id);
            if (!result) return NotFound();
            return Ok("Site deleted successfully");
        }

    }           


}