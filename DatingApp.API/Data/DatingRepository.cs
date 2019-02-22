using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext _context;

        public DatingRepository(DataContext context)
        {
            _context = context;
        }
        public void Add<T>(T entity) where T : class
        {
            _context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            _context.Remove(entity);
        }

        public async Task<Like> GetLike(int userId, int recipientId)
        {
            return await _context.Likes.FirstOrDefaultAsync(item => item.LikerId == userId && item.LikeeId == recipientId);
        }

        public async Task<Photo> GetMainPhotoForUser(int userId)
        {
            return await _context.Photos.FirstOrDefaultAsync(item => item.UserId == userId && item.isMain);
        }

        public async Task<Photo> GetPhoto(int id)
        {
            var photo = await _context.Photos.FirstOrDefaultAsync(item => item.Id == id);
            return photo;
        }

        public async Task<User> GetUser(int id)
        {
            var user = await _context.Users.Include(item => item.Photos)
                .FirstOrDefaultAsync(item => item.Id == id);
            return user;
        }

        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = _context.Users.Include(item => item.Photos).OrderByDescending(item=>item.LastActive).AsQueryable();
            users = users.Where(item=>item.Id != userParams.UserId);

            if (!string.IsNullOrEmpty(userParams.Gender)) {
                users = users.Where(item=>item.Gender == userParams.Gender);
            }

            if (userParams.Likers) {
                var userLikers = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(item=>userLikers.Contains(item.Id));
            }

            if (userParams.Likees) {
                var userLikees = await GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(item=>userLikees.Contains(item.Id));
            }

            if (userParams.MinAge != 18 || userParams.MaxAge != 99) {
                var minDateOfBirth = DateTime.Today.AddYears(-userParams.MaxAge - 1);
                var maxDateOfBirth = DateTime.Today.AddYears(-userParams.MinAge);

                users = users.Where(item => item.DateOfBirth >= minDateOfBirth && item.DateOfBirth <= maxDateOfBirth);
            }

            if (!string.IsNullOrEmpty(userParams.OrderBy)) {
                switch (userParams.OrderBy) {
                    case "created":
                        users = users.OrderByDescending(item=>item.Created);
                        break;
                    default:
                        users = users.OrderByDescending(item=>item.LastActive);
                        break;
                }
            }

            return await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }

        private async Task<IEnumerable<int>> GetUserLikes(int id, bool likers) {
            var user = await _context.Users
                .Include(item=>item.Likers)
                .Include(item=>item.Likees)
                .FirstOrDefaultAsync(item=>item.Id == id);

            if (likers) {
                return user.Likers.Where(item=>item.LikeeId == id).Select(item=>item.LikerId);
            } else {
                return user.Likees.Where(item=>item.LikerId == id).Select(item=>item.LikeeId);
            }
        }

        public async Task<bool> SaveAll()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}