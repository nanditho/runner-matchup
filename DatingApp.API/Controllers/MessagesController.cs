using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
  [ServiceFilter(typeof(LogUserActivity))]
  [Authorize]
  [Route("api/users/{userId}/[controller]")]
  [ApiController]
  public class MessagesController : ControllerBase
  {
    private readonly IDatingRepository _repository;
    private readonly IMapper _mapper;

    public MessagesController(IDatingRepository repository, IMapper mapper)
    {
      _repository = repository;
      _mapper = mapper;
    }

    [HttpGet("{id}", Name = "GetMessage")]
    public async Task<IActionResult> GetMessage(int userId, int id)
    {
      if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
        return this.Unauthorized();

      var messageFromRepo = await _repository.GetMessage(id);

      if (messageFromRepo == null)
        return NotFound();

      return Ok(messageFromRepo);
    }

    [HttpGet]
    public async Task<IActionResult> GetMessagesForUser(int userId, [FromQuery] MessageParams messageParams)
    {
      if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
        return this.Unauthorized();

      messageParams.UserId = userId;

      var messagesFromRepo = await _repository.GetMessagesForUser(messageParams);

      var messages = _mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

      Response.AddPagination(messagesFromRepo.CurrentPage, messagesFromRepo.PageSize, messagesFromRepo.TotalCount, messagesFromRepo.TotalPages);

      return Ok(messages);

    }

    [HttpGet("thread/{recipientId}")]
    public async Task<IActionResult> GetMessageThread(int userId, int recipeintId)
    {
      if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
        return this.Unauthorized();

      var messageFromRepo = await _repository.GetMessageThread(userId, recipeintId);

      var messageThread = _mapper.Map<IEnumerable<MessageToReturnDto>>(messageFromRepo);

      return Ok(messageThread);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMessage(int userId, MessageForCreationDto messageForCreationDto)
    {
      if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
        return this.Unauthorized();

      messageForCreationDto.SenderId = userId;

      var recipient = await _repository.GetUser(messageForCreationDto.RecipientId);

      if (recipient == null)
        return BadRequest("Could not find user");

      var message = _mapper.Map<Message>(messageForCreationDto);

      _repository.Add(message);

      var messageToReturn = _mapper.Map<MessageForCreationDto>(message);

      if (await _repository.SaveAll())
        return CreatedAtRoute("GetMessage", new { id = message.Id }, messageToReturn);

      throw new Exception("Createing the message failed on save");
    }
  }
}