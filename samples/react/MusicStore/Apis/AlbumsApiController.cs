﻿using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using MusicStore.Models;
using MusicStore.Infrastructure;

namespace MusicStore.Apis
{
    [Route("api/albums")]
    public class AlbumsApiController : Controller
    {
        private readonly MusicStoreContext _storeContext;

        public AlbumsApiController(MusicStoreContext storeContext)
        {
            _storeContext = storeContext;
        }

        [HttpGet]
        [NoCache]
        public async Task<ActionResult> Paged(int page = 1, int pageSize = 50, string sortBy = null)
        {
            await _storeContext.Genres.LoadAsync();
            await _storeContext.Artists.LoadAsync();

            var albums = await _storeContext.Albums
              .Include(a => a.Genre)
              .Include(a => a.Artist)
            .ToPagedListAsync(page, pageSize, sortBy,
                    a => a.Title,                                    // sortExpression
                    SortDirection.Ascending,                         // defaultSortDirection
                    a => Mapper.Map(a, new AlbumResultDto())); // selector

            return Json(albums);
        }

        [HttpGet("all")]
        [NoCache]
        public async Task<ActionResult> All()
        {
            var albums = await _storeContext.Albums
                .Include(a => a.Genre)
                .Include(a => a.Artist)
                .OrderBy(a => a.Title)
                .ToListAsync();

            return Json(albums.Select(a => Mapper.Map(a, new AlbumResultDto())));
        }

        [HttpGet("mostPopular")]
        [NoCache]
        public async Task<ActionResult> MostPopular(int count = 6)
        {
            count = count > 0 && count < 20 ? count : 6;
            var albums = await _storeContext.Albums
                .OrderByDescending(a => a.OrderDetails.Count())
                .Take(count)
                .ToListAsync();

            // TODO: Move the .Select() to end of albums query when EF supports it
            return Json(albums.Select(a => Mapper.Map(a, new AlbumResultDto())));
        }

        [HttpGet("{albumId:int}")]
        [NoCache]
        public async Task<ActionResult> Details(int albumId)
        {
            await _storeContext.Genres.LoadAsync();
            await _storeContext.Artists.LoadAsync();

            var album = await _storeContext.Albums
                .Include(a => a.Artist)
                .Include(a => a.Genre)
                .Where(a => a.AlbumId == albumId)
                .SingleOrDefaultAsync();

            var albumResult = Mapper.Map(album, new AlbumResultDto());

            // TODO: Add null checking and return 404 in that case

            return Json(albumResult);
        }

        [HttpPost]
        [Authorize("app-ManageStore")]
        public async Task<ActionResult> CreateAlbum([FromBody]AlbumChangeDto album)
        {
            if (!ModelState.IsValid)
            {
                // Return the model errors
                return BadRequest(ModelState);
            }

            // Save the changes to the DB
            var dbAlbum = new Album();
            _storeContext.Albums.Add(Mapper.Map(album, dbAlbum));
			await _storeContext.SaveChangesAsync();

            // TODO: Handle missing record, key violations, concurrency issues, etc.

            return new ObjectResult(new {
                Data = dbAlbum.AlbumId,
                Message = "Album created successfully."
            });
        }

        [HttpPut("{albumId:int}/update")]
        public async Task<ActionResult> UpdateAlbum(int albumId, [FromBody] AlbumChangeDto album)
        {
            if (!ModelState.IsValid)
            {
                // Return the model errors
                return BadRequest(ModelState);
            }

            var dbAlbum = await _storeContext.Albums.SingleOrDefaultAsync(a => a.AlbumId == albumId);

            if (dbAlbum == null)
            {
                return new ObjectResult(new {
                    Message = string.Format("The album with ID {0} was not found.", albumId)
                }) { StatusCode = 404 };
            }

            // Save the changes to the DB
            Mapper.Map(album, dbAlbum);
            await _storeContext.SaveChangesAsync();

            // TODO: Handle missing record, key violations, concurrency issues, etc.

            return new ObjectResult (new {
                Message = "Album updated successfully."
            });
        }

        [HttpDelete("{albumId:int}")]
        [Authorize("app-ManageStore")]
        public async Task<ActionResult> DeleteAlbum(int albumId)
        {
            var album = await _storeContext.Albums.SingleOrDefaultAsync(a => a.AlbumId == albumId);

            if (album != null)
            {
                _storeContext.Albums.Remove(album);

                // Save the changes to the DB
                await _storeContext.SaveChangesAsync();

                // TODO: Handle missing record, key violations, concurrency issues, etc.
            }

            return new ObjectResult (new {
                Message = "Album deleted successfully."
            });
        }
    }

    [ModelMetadataType(typeof(Album))]
    public class AlbumChangeDto
    {
        public int GenreId { get; set; }

        public int ArtistId { get; set; }

        public string Title { get; set; }

        public decimal Price { get; set; }

        public string AlbumArtUrl { get; set; }
    }

    public class AlbumResultDto : AlbumChangeDto
    {
        public AlbumResultDto()
        {
            Artist = new ArtistResultDto();
            Genre = new GenreResultDto();
        }

        public int AlbumId { get; set; }

        public ArtistResultDto Artist { get; private set; }

        public GenreResultDto Genre { get; private set; }
    }

    public class ArtistResultDto
    {
        public string Name { get; set; }
    }

    public class GenreResultDto
    {
        public string Name { get; set; }
    }
}
