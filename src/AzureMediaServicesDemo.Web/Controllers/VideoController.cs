using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AzureMediaServicesDemo.Web.Models;
using AzureMediaServicesDemo.Shared;
using Microsoft.Extensions.Logging;

namespace AzureMediaServicesDemo.Web.Controllers
{
    [Route("api/[controller]")]
    public class VideoController : Controller
    {
        private readonly IVideoService _videoService;
        private readonly ILogger<VideoController> _logger;

        public VideoController(ILogger<VideoController> logger, IVideoService videoService)
        {
            _logger = logger;
            _videoService = videoService;
        }
        [HttpGet("[action]")]
        public List<Video> GetVideos()
        {
            return _videoService.GetVideos().Result.ToList();
        }
    }
}