import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import axios from 'axios';
import { api } from '../services/apiClient'; // Fixed import path

export const Login: React.FC = () => {
    const [email, setEmail] = useState('admin@example.com');
    const [password, setPassword] = useState('admin123');
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);
    const navigate = useNavigate();
    const location = useLocation();

    // Roles:
    // admin@example.com / admin123
    // procurement@example.com / proc123
    // risk@example.com / risk123
    // viewer@example.com / view123

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setLoading(true);
        
        try {
            const response = await axios.post(`${import.meta.env.VITE_API_BASE_URL}/Auth/login`, {
                email,
                password
            });

            const { token, user } = response.data;
            
            // Store token and user data
            sessionStorage.setItem('auth_token', token);
            sessionStorage.setItem('user', JSON.stringify(user));
            
            // Set default headers for future requests
            api.defaults.headers.common['Authorization'] = `Bearer ${token}`;

            const from = location.state?.from?.pathname || '/';
            navigate(from, { replace: true });
        } catch (err: any) {
            console.error(err);
            setError(err.response?.data?.message || 'Invalid credentials or server error.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="d-flex align-items-center justify-content-center vh-100 bg-light w-100 p-3">
            <div className="card shadow-sm border-0 login-card">
                <div className="card-body p-4">
                    <div className="text-center mb-4">
                        <div className="bg-primary text-white rounded-circle d-inline-flex align-items-center justify-content-center mb-3" style={{width: '60px', height: '60px'}}>
                            <i className="bi bi-shield-lock fs-2"></i>
                        </div>
                        <h4 className="fw-bold">Vendor Management</h4>
                        <p className="text-muted small">Sign in to your account</p>
                    </div>

                    {error && <div className="alert alert-danger py-2 small">{error}</div>}

                    <form onSubmit={handleLogin}>
                        <div className="mb-3">
                            <label className="form-label small fw-bold">Email Address</label>
                            <input type="email" className="form-control" value={email} onChange={e => setEmail(e.target.value)} required />
                        </div>
                        
                        <div className="mb-4">
                            <label className="form-label small fw-bold">Password</label>
                            <input type="password" className="form-control" value={password} onChange={e => setPassword(e.target.value)} required />
                        </div>

                        <button type="submit" className="btn btn-primary w-100 py-2 fw-bold mb-3" disabled={loading}>
                            {loading ? <span className="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> : 'Sign In'}
                        </button>
                    </form>
                    
                    <div className="text-muted small mt-2">
                        <p className="mb-1 fw-bold">Demo Accounts:</p>
                        <ul className="mb-0 ps-3">
                            <li>Admin: <code>admin@example.com / admin123</code></li>
                            <li>Procurement: <code>procurement@example.com / proc123</code></li>
                            <li>Risk & Compliance: <code>risk@example.com / risk123</code></li>
                            <li>Viewer: <code>viewer@example.com / view123</code></li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>
    );
};