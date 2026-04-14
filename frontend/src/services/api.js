const API_BASE = '/api';

export const api = {
  async register(email, password) {
    const res = await fetch(`${API_BASE}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });
    if (!res.ok) {
      const error = await res.json();
      throw new Error(error.error || 'Registration failed');
    }
    return res.json();
  },

  async login(email, password) {
    const res = await fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });
    if (!res.ok) {
      const error = await res.json();
      throw new Error(error.error || 'Login failed');
    }
    const data = await res.json();
    localStorage.setItem('token', data.token);
    localStorage.setItem('userId', data.userId);
    return data;
  },

  async forgotPassword(email) {
    const res = await fetch(`${API_BASE}/auth/forgot-password`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email })
    });
    if (!res.ok) {
      throw new Error('Failed to send reset email');
    }
    return res.json();
  },

  async resetPassword(token, newPassword) {
    const res = await fetch(`${API_BASE}/auth/reset-password`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token, newPassword })
    });
    if (!res.ok) {
      const error = await res.json();
      throw new Error(error.error || 'Failed to reset password');
    }
    return res.json();
  },

  getToken() {
    return localStorage.getItem('token');
  },

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('userId');
  },

  async createLabReport(reportData) {
    const res = await fetch(`${API_BASE}/labreports`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getToken()}`
      },
      body: JSON.stringify(reportData)
    });
    if (!res.ok) throw new Error('Failed to create report');
    return res.json();
  },

  async getReports() {
    const res = await fetch(`${API_BASE}/labreports`, {
      headers: { 'Authorization': `Bearer ${this.getToken()}` }
    });
    if (!res.ok) throw new Error('Failed to fetch reports');
    return res.json();
  },

  async getReport(reportId) {
    const res = await fetch(`${API_BASE}/labreports/${reportId}`, {
      headers: { 'Authorization': `Bearer ${this.getToken()}` }
    });
    if (!res.ok) throw new Error('Failed to fetch report');
    return res.json();
  },

  async deleteReport(reportId) {
    const res = await fetch(`${API_BASE}/labreports/${reportId}`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${this.getToken()}` }
    });
    if (!res.ok) throw new Error('Failed to delete report');
    return res.json();
  },

  async exportExcel(reportId) {
    const res = await fetch(`${API_BASE}/exports/excel/${reportId}`, {
      headers: { 'Authorization': `Bearer ${this.getToken()}` }
    });
    if (!res.ok) throw new Error('Failed to export Excel');
    const blob = await res.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `WellnessReport_${new Date().toISOString().split('T')[0]}.xlsx`;
    a.click();
  },

  async exportPdf(reportId) {
    const res = await fetch(`${API_BASE}/exports/pdf/${reportId}`, {
      headers: { 'Authorization': `Bearer ${this.getToken()}` }
    });
    if (!res.ok) throw new Error('Failed to export PDF');
    const blob = await res.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `WellnessReport_${new Date().toISOString().split('T')[0]}.html`;
    a.click();
  },

  async getAllMarkerNames() {
    const res = await fetch(`${API_BASE}/labreports/markers/all`, {
      headers: { 'Authorization': `Bearer ${this.getToken()}` }
    });
    if (!res.ok) throw new Error('Failed to fetch marker names');
    return res.json();
  },

  async getMarkerTrends(markerName) {
    const res = await fetch(`${API_BASE}/labreports/markers/${encodeURIComponent(markerName)}/trends`, {
      headers: { 'Authorization': `Bearer ${this.getToken()}` }
    });
    if (!res.ok) throw new Error('Failed to fetch trends');
    return res.json();
  }
};