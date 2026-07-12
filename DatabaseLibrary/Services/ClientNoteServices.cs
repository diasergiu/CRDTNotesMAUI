using DatabaseLibrary.Entities;
using DatabaseLibrary.WrapperClasses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace DatabaseLibrary.Services
{
    public class ClientNoteServices
    {
        
        private HttpClient _httpClient;
        private string _baseUrl;
        public ClientNoteServices(string serverUrl)
        {
                _baseUrl = serverUrl;
                _httpClient = new HttpClient()
                {
                    BaseAddress = new Uri(_baseUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };
        }
        
        public async Task<ApiResult<List<Note>>> SendAndReceiveNoteUpdates(List<Note> flaggedNotes, string username, string password)
        {
            try
            {
                string url = $"{_baseUrl}?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";
                var response = await _httpClient.PostAsJsonAsync(url, flaggedNotes);
                if (response.IsSuccessStatusCode)
                {
                    var updatedNotes = await response.Content.ReadFromJsonAsync<List<Note>>();
                    return ApiResult<List<Note>>.Success(updatedNotes);
                }
                else
                {
                    Console.WriteLine($"Server returned error: {response.StatusCode}");
                    return ApiResult<List<Note>>.Failure($"Server returned error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notes to server: {ex.Message}");
                return ApiResult<List<Note>>.Failure($"Error sending notes to server: {ex.Message}");
            }
        }

    }
}
