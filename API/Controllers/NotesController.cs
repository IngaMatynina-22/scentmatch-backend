using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;

namespace ScentMatch.API.Controllers;

[ApiController]
[Route("api/notes")]
[AllowAnonymous]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;

    public NotesController(INoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NoteDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _noteService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NoteDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await _noteService.GetByIdAsync(id, cancellationToken));
    }
}
