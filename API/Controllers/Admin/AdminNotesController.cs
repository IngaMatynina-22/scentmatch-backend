using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Application.Catalog.Interfaces;
using ScentMatch.Domain.Constants;

namespace ScentMatch.API.Controllers.Admin;

[ApiController]
[Route("api/admin/notes")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminNotesController : ControllerBase
{
    private readonly INoteService _noteService;

    public AdminNotesController(INoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpPost]
    public async Task<ActionResult<NoteDto>> Create(
        [FromBody] CreateNoteRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _noteService.CreateAsync(request, cancellationToken);
        return Created($"/api/notes/{created.Id}", created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<NoteDto>> Update(
        int id,
        [FromBody] UpdateNoteRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _noteService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _noteService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
