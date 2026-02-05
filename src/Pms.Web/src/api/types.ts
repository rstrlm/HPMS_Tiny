// Room types
export type RoomStatus = 0 | 1 | 2 | 3 | 4 | 5 | string;

export type RoomDto = {
  id: string;
  roomNumber: string;
  roomTypeId: string;
  roomTypeName?: string | null;
  isActive: boolean;
  currentStatus: RoomStatus;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type CreateRoomRequest = {
  roomNumber: string;
  roomTypeId: string;
};

export type UpdateRoomRequest = {
  roomNumber: string;
  roomTypeId: string;
  isActive: boolean;
  currentStatus: number;
};

export type RoomTypeDto = {
  id: string;
  name: string;
  description?: string | null;
  capacity: number;
  basePrice: number;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type CreateRoomTypeRequest = {
  name: string;
  description?: string;
  capacity: number;
  basePrice: number;
};

export type UpdateRoomTypeRequest = {
  name: string;
  description?: string;
  capacity: number;
  basePrice: number;
};

export type RoomStateBlockType = 0 | 1 | string;

export type RoomStateBlockDto = {
  id: string;
  roomId: string;
  roomNumber?: string | null;
  startAtUtc: string;
  endAtUtc: string;
  type: RoomStateBlockType;
  note?: string | null;
  createdByStaffId?: string | null;
  createdByStaffName?: string | null;
  createdAtUtc: string;
};

// Cleaning task types
export type CleaningTaskStatus = 0 | 1 | 2 | 3 | string;
export type CleaningTaskType = 0 | 1 | 2 | string;

export type CleaningTaskDto = {
  id: string;
  roomId: string;
  roomNumber?: string | null;
  taskType: CleaningTaskType;
  status: CleaningTaskStatus;
  scheduledDate: string;
  assignedToStaffId?: string | null;
  assignedToStaffName?: string | null;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  notes?: string | null;
  skippedReason?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type CleaningTaskSummaryDto = {
  date: string;
  total: number;
  pending: number;
  inProgress: number;
  completed: number;
  skipped: number;
};

export type CreateCleaningTaskRequest = {
  roomId: string;
  taskType: number;
  scheduledDate: string;
  notes?: string;
};

export type UpdateCleaningTaskRequest = {
  assignedToStaffId?: string;
  notes?: string;
};

export type AssignTaskRequest = {
  staffId: string;
};

export type SkipTaskRequest = {
  reason?: string;
};

// Appointment types
export type AppointmentStatus = 0 | 1 | 2 | 3 | string;

export type AppointmentDto = {
  id: string;
  treatmentTypeId: string;
  treatmentTypeName?: string | null;
  treatmentRoomId: string;
  treatmentRoomName?: string | null;
  therapistId?: string | null;
  therapistName?: string | null;
  customerId?: string | null;
  customerName?: string | null;
  reservationId?: string | null;
  startAtUtc: string;
  endAtUtc: string;
  seatsUsed: number;
  status: AppointmentStatus;
  notes?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type CreateAppointmentRequest = {
  treatmentTypeId: string;
  treatmentRoomId: string;
  therapistId?: string;
  customerId?: string;
  reservationId?: string;
  startAtUtc: string;
  durationMinutes: number;
  seatsUsed?: number;
  notes?: string;
};

export type UpdateAppointmentRequest = {
  startAtUtc?: string;
  durationMinutes?: number;
  therapistId?: string;
  notes?: string;
};

export type UpdateAppointmentStatusRequest = {
  status: number;
};

// Staff types
export type StaffProfileDto = {
  id: string;
  keycloakUserId: string;
  displayName: string;
  email?: string | null;
  skills?: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type CreateStaffProfileRequest = {
  keycloakUserId: string;
  displayName: string;
  email?: string;
  skills?: string;
};

export type UpdateStaffProfileRequest = {
  displayName?: string;
  email?: string;
  skills?: string;
  isActive?: boolean;
};

export type CreateStaffWithKeycloakRequest = {
  username: string;
  password: string;
  displayName: string;
  email: string;
  skills?: string;
  roles?: string[];
};

// Treatment types
export type TreatmentTypeDto = {
  id: string;
  name: string;
  description?: string | null;
  durationMinutes: number;
  bufferMinutes: number;
  basePrice: number;
  isActive: boolean;
  requiresTherapist: boolean;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export type CreateTreatmentTypeRequest = {
  name: string;
  description?: string;
  durationMinutes: number;
  bufferMinutes: number;
  basePrice: number;
  requiresTherapist?: boolean;
};

export type UpdateTreatmentTypeRequest = {
  name: string;
  description?: string;
  durationMinutes: number;
  bufferMinutes: number;
  basePrice: number;
  isActive: boolean;
  requiresTherapist: boolean;
};

export type TreatmentRoomDto = {
  id: string;
  name: string;
  description?: string | null;
  capacity: number;
  isActive: boolean;
  createdAtUtc?: string;
  updatedAtUtc?: string;
};

export type CreateTreatmentRoomRequest = {
  name: string;
  description?: string;
  capacity: number;
};

export type UpdateTreatmentRoomRequest = {
  name: string;
  description?: string;
  capacity: number;
  isActive: boolean;
};

export type TimeSlotDto = {
  startUtc: string;
  endUtc: string;
  availableCapacity: number;
};

// Customer types
export type CustomerDto = {
  id: string;
  name: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  notes?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type CreateCustomerRequest = {
  name: string;
  phone?: string;
  email?: string;
  address?: string;
  notes?: string;
};

export type UpdateCustomerRequest = {
  name: string;
  phone?: string;
  email?: string;
  address?: string;
  notes?: string;
};

// Reservation types
export type ReservationStatus = 0 | 1 | 2 | 3 | 4 | string;

export type ReservationDto = {
  id: string;
  customerId: string;
  customerName?: string | null;
  checkInDate: string;
  checkOutDate: string;
  status: ReservationStatus;
  notes?: string | null;
  numberOfGuests: number;
  roomAssignments: RoomAssignmentDto[];
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type RoomAssignmentDto = {
  id: string;
  roomId: string;
  roomNumber?: string | null;
  roomTypeName?: string | null;
  fromDate: string;
  toDate: string;
};

export type CreateReservationRequest = {
  customerId?: string;
  checkInDate: string;
  checkOutDate: string;
  numberOfGuests: number;
  notes?: string;
  roomAssignments: CreateRoomAssignmentRequest[];
  newCustomer?: CreateCustomerRequest;
  appointments?: CreateReservationAppointmentRequest[];
};

export type CreateReservationAppointmentRequest = {
  treatmentTypeId: string;
  treatmentRoomId: string;
  therapistStaffId?: string;
  startAtUtc: string;
  seatsUsed?: number;
  notes?: string;
};

export type CreateRoomAssignmentRequest = {
  roomId: string;
  fromDate: string;
  toDate: string;
};

export type UpdateReservationRequest = {
  checkInDate?: string;
  checkOutDate?: string;
  numberOfGuests?: number;
  notes?: string;
};

export type ChangeReservationStatusRequest = {
  status: number;
};

export type RoomAvailabilityInfo = {
  roomId: string;
  roomNumber: string;
  roomTypeId: string;
  roomTypeName: string;
  isAvailable: boolean;
  conflictingDates?: string[];
};

export type PlaceHoldRequest = {
  roomId: string;
  fromDate: string;
  toDate: string;
  holdMinutes?: number;
};

export type HoldDto = {
  id: string;
  roomId: string;
  fromDate: string;
  toDate: string;
  expiresAtUtc: string;
};

// Billing types
export type FolioStatus = 0 | 1 | 2 | string;
export type ChargeType = 0 | 1 | 2 | string;
export type PaymentMethod = 0 | 1 | 2 | string;
export type PaymentStatus = 0 | 1 | 2 | 3 | string;
export type InvoiceStatus = 0 | 1 | string;

export type ChargeDto = {
  id: string;
  type: ChargeType;
  description: string;
  quantity: number;
  unitPrice: number;
  vatRate: number;
  subTotal: number;
  vatAmount: number;
  total: number;
  createdAtUtc: string;
};

export type PaymentDto = {
  id: string;
  amount: number;
  method: PaymentMethod;
  status: PaymentStatus;
  providerReference?: string | null;
  createdAtUtc: string;
};

export type FolioDto = {
  id: string;
  customerId: string;
  customerName: string;
  reservationId?: string | null;
  status: FolioStatus;
  subTotal: number;
  vatTotal: number;
  grandTotal: number;
  totalPaid: number;
  balance: number;
  charges: ChargeDto[];
  payments: PaymentDto[];
  createdAtUtc: string;
};

export type FolioSummaryDto = {
  id: string;
  customerId: string;
  customerName: string;
  reservationId?: string | null;
  status: FolioStatus;
  grandTotal: number;
  totalPaid: number;
  balance: number;
  createdAtUtc: string;
};

export type CreateFolioRequest = {
  customerId: string;
  reservationId?: string;
};

export type CreateChargeRequest = {
  type: number;
  description: string;
  quantity: number;
  unitPrice: number;
  vatRate?: number;
};

export type CreatePaymentRequest = {
  amount: number;
  method: number;
  providerReference?: string;
};

export type InvoiceDto = {
  id: string;
  folioId: string;
  invoiceNumber: string;
  issuedAtUtc: string;
  status: InvoiceStatus;
  subTotal: number;
  vatTotal: number;
  grandTotal: number;
};

// Branding types
export type BrandingDto = {
  id: string;
  companyName: string;
  companyLegalName: string;
  tagline: string;
  address: string;
  email: string;
  phone: string;
  taxId: string;
  bankName: string;
  iban: string;
  bic: string;
  updatedAtUtc: string;
};

export type UpdateBrandingRequest = {
  companyName: string;
  companyLegalName: string;
  tagline: string;
  address: string;
  email: string;
  phone: string;
  taxId: string;
  bankName: string;
  iban: string;
  bic: string;
};

export type BrandingChangeLogDto = {
  id: string;
  oldValues?: string | null;
  newValues?: string | null;
  performedByStaffId?: string | null;
  performedByKeycloakId?: string | null;
  createdAtUtc: string;
};
