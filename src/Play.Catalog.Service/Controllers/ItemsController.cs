using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Common;
using MassTransit;
using Play.Catalog.Contracts;

namespace Play.Catalog.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
       private readonly IRepository<Item> _itemsRepository;
       private readonly IPublishEndpoint _publishEdpoint;
       public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEdpoint)
       {
        _itemsRepository = itemsRepository;
        _publishEdpoint = publishEdpoint;
       }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAsync()
        {
            var items = (await _itemsRepository.GetAllAsync())
            .Select(item => item.AsDto());
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);
            if(item == null)
            {
                return NotFound();
            }
            return item.AsDto();
        }

        [HttpPost]
        public async Task<ActionResult<ItemDto>> Post(CreateItemDto createItemDto)
        {
            var item = new Item
            {
                Name = createItemDto.Name,
                Description = createItemDto.Description,
                Price = createItemDto.Price,
                CreatedDate = DateTimeOffset.UtcNow
            };

            await _itemsRepository.CreateAsync(item);

            _publishEdpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description));

            return CreatedAtAction(nameof(GetByIdAsync), new {id=item.AsDto().id}, item.AsDto());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
        {
            
            var existingItem = await _itemsRepository.GetAsync(id);
            if(existingItem == null)
            {
                return NotFound();
            }
           existingItem.Name = updateItemDto.Name;
           existingItem.Description = updateItemDto.Description;
           existingItem.Price = updateItemDto.Price;

            await _itemsRepository.UpdateAsync(existingItem);
            _publishEdpoint.Publish(new CatalogItemUpdated(existingItem.Id, existingItem.Name, existingItem.Description));
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
           var existingItem = await _itemsRepository.GetAsync(id);
            if(existingItem == null)
            {
                return NotFound();
            }
            await _itemsRepository.RemoveAsync(existingItem.Id);
            _publishEdpoint.Publish(new CatalogItemDeleted(id));
            return NoContent();
        }
    }
}