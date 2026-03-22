using Microsoft.AspNetCore.Mvc;

namespace PlayoutServer.Core.Controllers
{
    /// <summary>
    /// Views für PlayoutServer Web-GUI
    /// </summary>
    [Route("playout")]
    public class PlayoutViewController : Controller
    {
        /// <summary>GET /playout/editor - Playlist Editor Page</summary>
        [HttpGet("editor")]
        public IActionResult Editor()
        {
            return View("PlaylistEditor");
        }

        /// <summary>GET /playout - Home/Dashboard</summary>
        [HttpGet("")]
        public IActionResult Index() 
        {
            return Content("PlayoutServer Running - USE REST API /api/playout/*", "text/html");
        }
    }
}
