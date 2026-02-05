import { fetchJson } from "./client";
import type { CustomerDto, CreateCustomerRequest, UpdateCustomerRequest } from "./types";

export const getCustomers = async (query?: string): Promise<CustomerDto[]> => {
  const params = new URLSearchParams();
  if (query) {
    params.set("q", query);
  }
  const qs = params.toString();
  return fetchJson<CustomerDto[]>(qs ? `/customers?${qs}` : "/customers");
};

export const getCustomer = async (id: string): Promise<CustomerDto> => {
  return fetchJson<CustomerDto>(`/customers/${id}`);
};

export const createCustomer = async (request: CreateCustomerRequest): Promise<CustomerDto> => {
  return fetchJson<CustomerDto>("/customers", {
    method: "POST",
    body: JSON.stringify(request)
  });
};

export const updateCustomer = async (
  id: string,
  request: UpdateCustomerRequest
): Promise<CustomerDto> => {
  return fetchJson<CustomerDto>(`/customers/${id}`, {
    method: "PATCH",
    body: JSON.stringify(request)
  });
};

export const deleteCustomer = async (id: string): Promise<void> => {
  await fetchJson(`/customers/${id}`, { method: "DELETE" });
};
