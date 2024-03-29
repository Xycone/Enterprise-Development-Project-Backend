﻿using EDP_Project_Backend.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AutoMapper;

namespace EDP_Project_Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TierController : ControllerBase
    {
        private readonly MyDbContext _context;
        private readonly IMapper _mapper;
        public TierController(MyDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // Used to retrieve the authenticated user's userid
        // prob wont need it but leave it here in case i need it in the future
        private int GetUserId()
        {
            return Convert.ToInt32(User.Claims
            .Where(c => c.Type == ClaimTypes.NameIdentifier)
            .Select(c => c.Value).SingleOrDefault());
        }

        // Returns total number of tiers
        [HttpGet("get-tiers")]
        public IActionResult GetNumberOfTiers()
        {
            int numberOfTiers = _context.Tiers.Count();
            return Ok(numberOfTiers);
        }

        // Returns all the available tiers
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<TierDTO>), StatusCodes.Status200OK)]
        public IActionResult GetAll(string? search)
        {
            IQueryable<Tier> result = _context.Tiers;
            if (search != null)
            {
                result = result.Where(x => x.TierName.Contains(search));
            }
            var list = result.OrderBy(x => x.TierPosition).ToList();
            IEnumerable<TierDTO> data = list.Select(t => _mapper.Map<TierDTO>(t));
            return Ok(data);
        }

        // Returns tier info of the tier by id
        [HttpGet("{id}"), Authorize]
        [ProducesResponseType(typeof(TierDTO), StatusCodes.Status200OK)]
        public IActionResult GetTier(int id)
        {
            var tier = _context.Tiers.Find(id);
            if (tier == null)
            {
                return NotFound();
            }
            TierDTO data = _mapper.Map<TierDTO>(tier);
            return Ok(data);
        }

        // Fields needed TierName, TierBookings and TierSpendings
        [HttpPost, Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(TierDTO), StatusCodes.Status200OK)]
        public IActionResult AddTier(AddTierRequest tier)
        {
            var now = DateTime.Now;

            // Retrieves the current highest tier position in the db. 
            // If nothing in the db, the currentHighestTier will be given the null value
            int? currentHighestTier = _context.Tiers.Max(t => (int?)t.TierPosition);

            // By default, newly created tier will, be in the last tier position
            var myTier = new Tier()
            {
                TierName = tier.TierName.Trim(),
                TierBookings = tier.TierBookings,
                TierSpendings = tier.TierSpendings,
                // if currentHighestTier is null the currentHighestTier will be given a default value of 0
                TierPosition = (currentHighestTier ?? 0) + 1,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Tiers.Add(myTier);
            _context.SaveChanges();

            Tier? newTier = _context.Tiers.FirstOrDefault(t => t.Id == myTier.Id);
            TierDTO tierDTO = _mapper.Map<TierDTO>(newTier);
            return Ok(tierDTO);
        }


        // Accepts id of tier to be updated as a parameter
        // Fields needed TierName, TierBookings, TierSpendings and TierPosition
        [HttpPut("{id}"), Authorize(Roles = "admin")]
        public IActionResult UpdateTier(int id, UpdateTierRequest tier)
        {
            var myTier = _context.Tiers.Find(id);
            if (myTier == null)
            {
                return NotFound();
            }

            // Prevents the updated tierposition from being greater than the number of 
            if (tier.TierPosition > _context.Tiers.Count())
            {
                return BadRequest("Invalid TierPosition. Tier position cannot be greater than the number of tiers");
            }

            if (tier.TierName != null)
            {
                myTier.TierName = tier.TierName.Trim();
            }
            if (tier.TierBookings > 0)
            {
                myTier.TierBookings = tier.TierBookings;
            }
            if (tier.TierSpendings > 0)
            {
                myTier.TierSpendings = tier.TierSpendings;
            }

            // Check if TierPosition is being changed in this update request
            if (myTier.TierPosition != tier.TierPosition)
            {

                // New TierPosition of the tier
                var newPosition = tier.TierPosition;

                if (newPosition > 0)
                {
                    if (newPosition < myTier.TierPosition)
                    {
                        // If the newPosition is lower than the original position (e.g. 3 move to 1),
                        // the other tiers with tier position greater than or equal to the new position will be moved back by one to make space
                        var affectedTiersLower = _context.Tiers.Where(t => t.TierPosition >= newPosition && t.TierPosition < myTier.TierPosition && t.Id != myTier.Id).ToList();
                        foreach (var affectedTier in affectedTiersLower)
                        {
                            affectedTier.TierPosition++;
                        }
                    }
                    else
                    {
                        // If the newPosition is higher than the original position (e.g. 1 move to 3),
                        // the other tiers with tier position greater than or equal to the new position will be moved forward by one to fill the gap
                        var affectedTiersHigher = _context.Tiers.Where(t => t.TierPosition > myTier.TierPosition && t.TierPosition <= newPosition && t.Id != myTier.Id).ToList();
                        foreach (var affectedTier in affectedTiersHigher)
                        {
                            affectedTier.TierPosition--;
                        }
                    }

                    myTier.TierPosition = newPosition;
                }
            }

            myTier.UpdatedAt = DateTime.Now;
            _context.SaveChanges();
            return Ok();
        }

        // Important!
        // Delete request will move tiers with tier position greater than that of the tier being deleted forward by one in order to fill the gap
        // Users tied to the tier being deleted will be bumped up a tier
        // Request fails if there are no availble tiers to bump the user up to
        // Only accept id of tier to be deleted as a parameter, no request body
        [HttpDelete("{id}"), Authorize(Roles = "admin")]
        public IActionResult DeleteTier(int id)
        {
            var myTier = _context.Tiers.Find(id);
            if (myTier == null)
            {
                return NotFound();
            }

            var nextHighestTier = _context.Tiers.Where(t => t.TierPosition > myTier.TierPosition && t.Id != myTier.Id).OrderBy(t => t.TierPosition).FirstOrDefault();
            var affectedUsers = _context.Users.Where(u => u.TierId == myTier.Id).ToList();
            // Only returns badrequest when there are users affected but no tier to bump them up to
            if (nextHighestTier == null && affectedUsers.Count != 0)
            {
                return BadRequest("No available tiers to upgrade users to.");
            }

            // Find the tiers with tier position greater than the tier being deleted
            // Tier that is being delted is deleted
            // Affected tiers have their tier position moved forward by one to fill in the gap in position left by deleting said tier
            var affectedTiers = _context.Tiers.Where(t => t.TierPosition > myTier.TierPosition).ToList();
            foreach (var tier in affectedTiers)
            {
                tier.TierPosition--;
            }

            // Bumps users currently in the tier being deleted up a tier 
            foreach (var user in affectedUsers)
            {
                if (nextHighestTier != null)
                {
                    user.TierId = nextHighestTier.Id;
                }
            }

            _context.Tiers.Remove(myTier);
            _context.SaveChanges();
            return Ok();
        }

    }
}
