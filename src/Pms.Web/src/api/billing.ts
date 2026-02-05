import { fetchJson } from "./client";
import { getAccessToken } from "../state/auth";
import { buildApiUrl } from "../lib/config";
import type {
  FolioDto,
  FolioSummaryDto,
  ChargeDto,
  PaymentDto,
  InvoiceDto,
  CreateFolioRequest,
  CreateChargeRequest,
  CreatePaymentRequest
} from "./types";

// Folios
export const getFolio = async (id: string): Promise<FolioDto> => {
  return fetchJson<FolioDto>(`/folios/${id}`);
};

export const getFoliosByCustomer = async (customerId: string): Promise<FolioSummaryDto[]> => {
  return fetchJson<FolioSummaryDto[]>(`/folios/by-customer/${customerId}`);
};

export const getFolioByReservation = async (reservationId: string): Promise<FolioDto> => {
  return fetchJson<FolioDto>(`/folios/by-reservation/${reservationId}`);
};

export const createFolio = async (request: CreateFolioRequest): Promise<FolioDto> => {
  return fetchJson<FolioDto>("/folios", {
    method: "POST",
    body: JSON.stringify(request)
  });
};

// Charges
export const addCharge = async (folioId: string, request: CreateChargeRequest): Promise<ChargeDto> => {
  return fetchJson<ChargeDto>(`/folios/${folioId}/charges`, {
    method: "POST",
    body: JSON.stringify(request)
  });
};

export const removeCharge = async (chargeId: string): Promise<void> => {
  await fetchJson(`/folios/charges/${chargeId}`, { method: "DELETE" });
};

// Payments
export const addPayment = async (
  folioId: string,
  request: CreatePaymentRequest
): Promise<PaymentDto> => {
  return fetchJson<PaymentDto>(`/folios/${folioId}/payments`, {
    method: "POST",
    body: JSON.stringify(request)
  });
};

// Invoices
export const issueInvoice = async (folioId: string): Promise<InvoiceDto> => {
  return fetchJson<InvoiceDto>(`/folios/${folioId}/issue-invoice`, {
    method: "POST"
  });
};

export const getInvoicesByFolio = async (folioId: string): Promise<InvoiceDto[]> => {
  return fetchJson<InvoiceDto[]>(`/folios/${folioId}/invoices`);
};

export const voidInvoice = async (invoiceId: string): Promise<InvoiceDto> => {
  return fetchJson<InvoiceDto>(`/folios/invoices/${invoiceId}/void`, {
    method: "POST"
  });
};

// Close folio
export const closeFolio = async (folioId: string): Promise<FolioDto> => {
  return fetchJson<FolioDto>(`/folios/${folioId}/close`, {
    method: "POST"
  });
};

// Cancel folio
export const cancelFolio = async (folioId: string): Promise<FolioDto> => {
  return fetchJson<FolioDto>(`/folios/${folioId}/cancel`, {
    method: "POST"
  });
};

// Merge folios
export const mergeFolios = async (
  targetFolioId: string,
  sourceFolioIds: string[]
): Promise<FolioDto> => {
  return fetchJson<FolioDto>("/folios/merge", {
    method: "POST",
    body: JSON.stringify({ targetFolioId, sourceFolioIds })
  });
};

// Download invoice PDF
export const downloadInvoicePdf = async (invoiceId: string): Promise<void> => {
  const token = getAccessToken();

  const response = await fetch(buildApiUrl(`/folios/invoices/${invoiceId}/pdf`), {
    headers: token ? { Authorization: `Bearer ${token}` } : {}
  });

  if (!response.ok) {
    throw new Error("Failed to download invoice PDF");
  }

  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `invoice-${invoiceId}.pdf`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  window.URL.revokeObjectURL(url);
};
