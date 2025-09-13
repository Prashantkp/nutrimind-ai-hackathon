// Auth related data contracts (reconstructed after accidental deletion)

export interface RegisterRequest {
	email: string;
	password: string;
	firstName: string;
	lastName: string;
	phoneNumber?: string; // optional in model but provided by register component
}

export interface LoginRequest {
	email: string;
	password: string;
}

export interface RefreshTokenRequest {
	refreshToken: string;
}

export interface UserDto {
	id: string;
	email: string;
	firstName: string;
	lastName: string;
	// Indicates whether user already created a profile (used by guards)
	hasProfile: boolean;
	// Optional timestamps if backend provides
	createdAt?: string;
	updatedAt?: string;
}

export interface AuthResponse {
	token: string;
	refreshToken: string;
	user: UserDto;
	expiresIn?: number; // seconds until expiry (optional)
}

// Generic API response wrapper as used in services
export interface ApiResponse<T> {
	success: boolean;
	message?: string;
	data?: T;
	errors?: string[];
}

