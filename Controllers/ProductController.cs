using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TI_Devops_2026_Bend_IAC.contexts;
using TI_Devops_2026_Bend_IAC.Dtos;
using TI_Devops_2026_Bend_IAC.Entities;

namespace TI_Devops_2026_Bend_IAC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController(MyDbContext context) : ControllerBase
    {

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromForm] ProductForm form)
        {

            var product = new Product
            {
                Name = form.name
            };

            var added = await context.Products.AddAsync(product);
            await context.SaveChangesAsync();

            return Ok(added);
        } 
    }
}
