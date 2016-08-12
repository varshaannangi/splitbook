﻿using Newtonsoft.Json;

using SplitBook.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SplitBook.Request
{
    class DeleteExpenseRequest : RestBaseRequest
    {
        public static String deleteExpenseURL = "delete_expense/";
        private int expenseId;

        public DeleteExpenseRequest(int id)
            : base()
        {
            expenseId = id;
        }

        public async void deleteExpense(Action<bool> CallbackOnSuccess, Action<HttpStatusCode> CallbackOnFailure)
        {
            try
            {
                HttpResponseMessage response = await client.PostAsync(deleteExpenseURL + expenseId.ToString(), null);
                JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                DeleteExpense result = Newtonsoft.Json.JsonConvert.DeserializeObject<DeleteExpense>(await response.Content.ReadAsStringAsync(), settings);
                if (result.success)
                    CallbackOnSuccess(result.success);
                else
                    CallbackOnFailure(response.StatusCode);
            }
            catch (Exception e)
            {
                CallbackOnFailure(HttpStatusCode.ServiceUnavailable);
            }
        }

        private class DeleteExpense
        {
            public Boolean success { get; set; }
            public Object error { get; set; }
        }
    }
}