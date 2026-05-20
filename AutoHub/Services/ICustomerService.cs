namespace AutoHub.API.Services;
using AutoHub.DTOs;
public interface ICustomerService
{
    Task<CustomerResponseDto> CreateCustomerAsync(CustomerCreateDto dto);
    Task<CustomerResponseDto> GetCustomerByIdAsync(int id);
    Task<List<CustomerResponseDto>> SearchCustomersAsync(string query);
}