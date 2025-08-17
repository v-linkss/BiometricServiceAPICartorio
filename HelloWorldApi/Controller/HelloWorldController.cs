// using Microsoft.AspNetCore.Mvc;

// namespace HelloWorldApi.Controllers
// {
//     [ApiController]
//     [Route("api/[controller]")]
//     public class HelloWorldController : ControllerBase
//     {
//         [HttpGet]
//         public string Get()
//         {
//             return "Hello World!";
//         }
//     }
// }

using Microsoft.AspNetCore.Mvc;
using HelloWorldApi.Services;

namespace HelloWorldApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HelloWorldController : ControllerBase
    {
        private readonly FileWatcherService _fileWatcherService;

        public HelloWorldController(FileWatcherService fileWatcherService)
        {
            _fileWatcherService = fileWatcherService;
        }

        [HttpGet]
        public string Get()
        {
            return "Hello World!";
        }
    }
}