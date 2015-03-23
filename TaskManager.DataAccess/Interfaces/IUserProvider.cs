﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace TaskManager.DataAccess.Interfaces
{
    public interface IUserProvider
    {
        UserProfile CurrentUser();
        bool IsAdmin();
        bool IsChief();
        bool IsRecipient();
        bool IsSender();
        bool IsMasterChief();
        List<UserProfile> GetAllUsers();
        IEnumerable<string> GetRolesForUser(string userName);
        UserProfile GetUserByLogin(string login);
        UserProfile GetUserById(int id);
        string[] GetRolesNamesArray(UserModel model);
        bool SaveEditedUser(UserModel model);
        bool DeleteUserById(int id);
        bool IsUserInAnyRole();
        int GetNewUsersCount();
        bool IsNewUser(UserProfile user);
        IEnumerable<UserProfile> GetChiefs();
        IEnumerable<SelectListItem> GetPrioritiesSelectedList(string selectedPriorityId, TaskManagerContext context = null);
        IEnumerable<SelectListItem> GetRecipientsSelectedList(string firstElementTitle, string selectedRecipientId, TaskManagerContext context = null);
        UserProfile CheckUser(string username);
        void SaveChages();
    }
}
