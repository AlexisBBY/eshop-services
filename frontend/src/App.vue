<script setup>
import { computed, onMounted, ref } from 'vue'
import jsPDF from 'jspdf'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5201'
const BASKET_API_URL = import.meta.env.VITE_BASKET_API_URL || 'http://localhost:8082'
const ORDERS_API_URL = import.meta.env.VITE_ORDERS_API_URL || 'http://localhost:5210'

// --- Navegacion simple por query string (sin vue-router): ?view=orders o ?view=order&id=xxx ---
const urlParams = new URLSearchParams(window.location.search)
const currentView = ref(urlParams.get('view') || 'catalog')
const initialOrderId = urlParams.get('id') || ''

function buildUrl(params) {
  const search = new URLSearchParams(params).toString()
  return `${window.location.origin}${window.location.pathname}?${search}`
}

function openOrdersPage() {
  window.open(buildUrl({ view: 'orders' }), '_blank')
}

function openOrderPage(id) {
  window.open(buildUrl({ view: 'order', id }), '_blank')
}

// ===================== CATALOGO =====================
const products = ref([])
const loading = ref(false)
const error = ref('')
const searchTerm = ref('')
const pageIndex = ref(1)
const pageSize = ref(5)
const totalCount = ref(0)
const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

const categoryFilter = ref('')
const minPriceFilter = ref('')
const maxPriceFilter = ref('')

const form = ref({
  id: null,
  name: '',
  description: '',
  category: '',
  imageFile: '',
  price: ''
})

const editingId = ref(null)
const submitting = ref(false)

async function loadProducts() {
  loading.value = true
  error.value = ''

  try {
    const params = new URLSearchParams({
      pageIndex: pageIndex.value,
      pageSize: pageSize.value
    })

    if (searchTerm.value.trim()) params.set('name', searchTerm.value.trim())
    if (categoryFilter.value.trim()) params.set('category', categoryFilter.value.trim())
    if (minPriceFilter.value !== '') params.set('minPrice', minPriceFilter.value)
    if (maxPriceFilter.value !== '') params.set('maxPrice', maxPriceFilter.value)

    const response = await fetch(`${API_URL}/products?${params.toString()}`)
    if (!response.ok) throw new Error('No se pudieron cargar los productos')

    const payload = await response.json()
    products.value = payload.data || []
    totalCount.value = payload.count || 0
  } catch (err) {
    error.value = err.message || 'Ocurrió un error'
  } finally {
    loading.value = false
  }
}

function resetForm() {
  form.value = { id: null, name: '', description: '', category: '', imageFile: '', price: '' }
  editingId.value = null
}

function editProduct(product) {
  editingId.value = product.id
  form.value = {
    id: product.id,
    name: product.name,
    description: product.description,
    category: product.category?.join(', ') || '',
    imageFile: product.imageFile,
    price: product.price
  }
}

async function saveProduct() {
  submitting.value = true
  error.value = ''

  try {
    const payload = {
      name: form.value.name,
      description: form.value.description,
      category: form.value.category.split(',').map((item) => item.trim()).filter(Boolean),
      imageFile: form.value.imageFile,
      price: Number(form.value.price)
    }

    const method = editingId.value ? 'PUT' : 'POST'
    if (editingId.value) payload.id = form.value.id

    const response = await fetch(`${API_URL}/products`, {
      method,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })

    if (!response.ok) throw new Error('No se pudo guardar el producto')

    resetForm()
    await loadProducts()
  } catch (err) {
    error.value = err.message || 'Ocurrió un error'
  } finally {
    submitting.value = false
  }
}

async function deleteProduct(product) {
  if (!confirm(`¿Deseas eliminar ${product.name}?`)) return

  try {
    const response = await fetch(`${API_URL}/products/by-name/${encodeURIComponent(product.name)}`, { method: 'DELETE' })
    if (!response.ok) throw new Error('No se pudo eliminar')
    await loadProducts()
  } catch (err) {
    error.value = err.message || 'Ocurrió un error'
  }
}

function goToPage(nextPage) {
  if (nextPage < 1 || nextPage > totalPages.value) return
  pageIndex.value = nextPage
  loadProducts()
}

function applyFilters() {
  pageIndex.value = 1
  loadProducts()
}

function clearFilters() {
  searchTerm.value = ''
  categoryFilter.value = ''
  minPriceFilter.value = ''
  maxPriceFilter.value = ''
  pageIndex.value = 1
  loadProducts()
}

// ===================== CARRITO (panel deslizable) =====================
const cartOpen = ref(false)
const basketUserName = ref('Alexis')
const basket = ref(null)
const basketLoading = ref(false)
const basketError = ref('')
const basketTotal = computed(() =>
  (basket.value?.items || []).reduce((sum, item) => sum + item.price * item.quantity, 0)
)
const cartItemCount = computed(() =>
  (basket.value?.items || []).reduce((sum, item) => sum + item.quantity, 0)
)

const checkoutLoading = ref(false)
const checkoutError = ref('')

function toggleCart() {
  cartOpen.value = !cartOpen.value
}

async function loadBasket() {
  if (!basketUserName.value.trim()) return

  basketLoading.value = true
  basketError.value = ''

  try {
    const response = await fetch(`${BASKET_API_URL}/basket/${encodeURIComponent(basketUserName.value.trim())}`)

    if (response.status === 404) {
      basket.value = { userName: basketUserName.value.trim(), items: [] }
      return
    }

    if (!response.ok) throw new Error('No se pudo cargar el carrito')

    const payload = await response.json()
    basket.value = payload.cart
  } catch (err) {
    basketError.value = err.message || 'Ocurrió un error'
  } finally {
    basketLoading.value = false
  }
}

async function saveBasket() {
  try {
    const response = await fetch(`${BASKET_API_URL}/basket`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ cart: basket.value })
    })
    if (!response.ok) throw new Error('No se pudo guardar el carrito')
  } catch (err) {
    basketError.value = err.message || 'Ocurrió un error'
  }
}

async function addToCart(product) {
  if (!basketUserName.value.trim()) {
    basketError.value = 'Escribe un usuario y carga el carrito primero'
    cartOpen.value = true
    return
  }

  if (!basket.value) basket.value = { userName: basketUserName.value.trim(), items: [] }

  const existing = basket.value.items.find((item) => item.productId === product.id)
  if (existing) {
    existing.quantity += 1
  } else {
    basket.value.items.push({
      productId: product.id,
      productName: product.name,
      price: product.price,
      quantity: 1,
      color: 'N/A'
    })
  }

  await saveBasket()
}

async function removeFromCart(item) {
  basket.value.items = basket.value.items.filter((i) => i.productId !== item.productId)
  await saveBasket()
}

async function clearBasket() {
  if (!basket.value) return

  try {
    const response = await fetch(`${BASKET_API_URL}/basket/${encodeURIComponent(basket.value.userName)}`, { method: 'DELETE' })
    if (!response.ok) throw new Error('No se pudo vaciar el carrito')
    basket.value.items = []
  } catch (err) {
    basketError.value = err.message || 'Ocurrió un error'
  }
}

async function realizarCompra() {
  if (!basket.value || basket.value.items.length === 0) {
    checkoutError.value = 'El carrito está vacío.'
    return
  }

  checkoutLoading.value = true
  checkoutError.value = ''

  try {
    const response = await fetch(`${ORDERS_API_URL}/api/orders`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID() },
      body: JSON.stringify({ customerId: basket.value.userName, basketId: basket.value.userName })
    })

    if (!response.ok) {
      const problem = await response.json().catch(() => null)
      throw new Error(problem?.detail || 'No se pudo generar la orden')
    }

    const order = await response.json()
    await clearBasket()
    cartOpen.value = false
    openOrderPage(order.id)
  } catch (err) {
    checkoutError.value = err.message || 'Ocurrió un error al generar la orden'
  } finally {
    checkoutLoading.value = false
  }
}

// ===================== PEDIDOS (vista: orders) =====================
const orders = ref([])
const ordersLoading = ref(false)
const ordersError = ref('')
const ordersCustomerFilter = ref('')

async function loadOrders() {
  ordersLoading.value = true
  ordersError.value = ''

  try {
    const url = ordersCustomerFilter.value.trim()
      ? `${ORDERS_API_URL}/api/orders/customer/${encodeURIComponent(ordersCustomerFilter.value.trim())}`
      : `${ORDERS_API_URL}/api/orders`

    const response = await fetch(url)
    if (!response.ok) throw new Error('No se pudieron cargar las órdenes')

    orders.value = await response.json()
  } catch (err) {
    ordersError.value = err.message || 'Ocurrió un error'
  } finally {
    ordersLoading.value = false
  }
}

function clearOrdersFilter() {
  ordersCustomerFilter.value = ''
  loadOrders()
}

function summarizeProducts(order) {
  const names = order.items.map((i) => i.productName)
  if (names.length <= 2) return names.join(', ')
  return `${names.slice(0, 2).join(', ')} +${names.length - 2} más`
}

// ===================== DETALLE DE ORDEN (vista: order) =====================
const orderDetail = ref(null)
const orderDetailError = ref('')
const orderDetailLoading = ref(false)

async function loadOrderDetail(id) {
  orderDetailLoading.value = true
  orderDetailError.value = ''

  try {
    const response = await fetch(`${ORDERS_API_URL}/api/orders/${id}`)
    if (!response.ok) throw new Error('No se encontró la orden')
    orderDetail.value = await response.json()
  } catch (err) {
    orderDetailError.value = err.message || 'Ocurrió un error'
  } finally {
    orderDetailLoading.value = false
  }
}

async function changeOrderDetailStatus(status) {
  if (!orderDetail.value) return

  try {
    const response = await fetch(`${ORDERS_API_URL}/api/orders/${orderDetail.value.id}/status`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ status })
    })
    if (!response.ok) {
      const problem = await response.json().catch(() => null)
      throw new Error(problem?.detail || 'No se pudo cambiar el estado')
    }
    orderDetail.value = await response.json()
  } catch (err) {
    orderDetailError.value = err.message || 'Ocurrió un error'
  }
}

function formatDate(iso) {
  return new Date(iso).toLocaleDateString()
}

function formatTime(iso) {
  return new Date(iso).toLocaleTimeString()
}

function descargarPdf(order) {
  const doc = new jsPDF()
  let y = 20

  doc.setFontSize(16)
  doc.text('Comprobante de orden de compra', 14, y)
  y += 10

  doc.setFontSize(10)
  doc.text(`Orden: ${order.id}`, 14, y); y += 6
  doc.text(`Cliente: ${order.customerId}`, 14, y); y += 6
  doc.text(`Fecha: ${formatDate(order.createdAt)}  Hora: ${formatTime(order.createdAt)}`, 14, y); y += 6
  doc.text(`Estado: ${order.status}`, 14, y); y += 10

  doc.setFontSize(11)
  doc.text('Producto', 14, y)
  doc.text('Cant.', 110, y)
  doc.text('Precio unit.', 135, y)
  doc.text('Total', 175, y)
  y += 4
  doc.line(14, y, 196, y)
  y += 6

  doc.setFontSize(10)
  for (const item of order.items) {
    doc.text(String(item.productName).slice(0, 45), 14, y)
    doc.text(String(item.quantity), 110, y)
    doc.text(`$${Number(item.unitPrice).toFixed(2)}`, 135, y)
    doc.text(`$${Number(item.lineTotal).toFixed(2)}`, 175, y)
    y += 7
  }

  y += 4
  doc.line(14, y, 196, y)
  y += 8

  doc.text(`Subtotal: $${Number(order.subtotal).toFixed(2)}`, 135, y); y += 6
  doc.text(`Impuesto: $${Number(order.tax).toFixed(2)}`, 135, y); y += 6
  doc.setFontSize(12)
  doc.text(`Total: $${Number(order.total).toFixed(2)}`, 135, y)

  doc.save(`orden-${order.id}.pdf`)
}

onMounted(() => {
  if (currentView.value === 'orders') {
    loadOrders()
  } else if (currentView.value === 'order' && initialOrderId) {
    loadOrderDetail(initialOrderId)
  } else {
    loadProducts()
    loadBasket()
  }
})
</script>

<template>
  <main class="page">
    <!-- ===================== VISTA: CATALOGO ===================== -->
    <section v-if="currentView === 'catalog'" class="panel">
      <div class="header">
        <div>
          <p class="eyebrow">Ejemplo web</p>
          <h1>Catálogo de productos</h1>
          <p class="subtitle">Busca, crea, actualiza y elimina productos usando la API del proyecto.</p>
        </div>
        <div class="header-actions">
          <button class="secondary" @click="openOrdersPage">📋 Pedidos</button>
          <button class="cart-icon-btn" @click="toggleCart">
            🛒
            <span v-if="cartItemCount" class="badge-count">{{ cartItemCount }}</span>
          </button>
        </div>
      </div>

      <div v-if="error" class="alert">{{ error }}</div>

      <div class="grid">
        <form class="card form-card" @submit.prevent="saveProduct">
          <h2>{{ editingId ? 'Actualizar producto' : 'Nuevo producto' }}</h2>

          <label>
            Nombre
            <input v-model="form.name" required placeholder="Ej. Laptop" />
          </label>

          <label>
            Descripción
            <textarea v-model="form.description" rows="3" placeholder="Descripción del producto"></textarea>
          </label>

          <label>
            Categorías
            <input v-model="form.category" placeholder="Tecnología, Oficina" />
          </label>

          <label>
            Imagen
            <input v-model="form.imageFile" placeholder="imagen.png" />
          </label>

          <label>
            Precio
            <input v-model="form.price" type="number" min="0" step="0.01" required />
          </label>

          <div class="actions">
            <button class="primary" type="submit" :disabled="submitting">
              {{ submitting ? 'Guardando...' : editingId ? 'Actualizar' : 'Crear' }}
            </button>
            <button type="button" class="secondary" @click="resetForm">Limpiar</button>
          </div>
        </form>

        <section class="card list-card">
          <div class="toolbar">
            <input v-model="searchTerm" placeholder="Buscar por nombre" @keyup.enter="applyFilters" />
            <input v-model="categoryFilter" placeholder="Categoría" @keyup.enter="applyFilters" />
          </div>

          <div class="toolbar">
            <input v-model="minPriceFilter" type="number" min="0" step="0.01" placeholder="Precio mínimo" @keyup.enter="applyFilters" />
            <input v-model="maxPriceFilter" type="number" min="0" step="0.01" placeholder="Precio máximo" @keyup.enter="applyFilters" />
            <button class="primary" @click="applyFilters">Filtrar</button>
            <button class="secondary" @click="clearFilters">Limpiar filtros</button>
          </div>

          <div class="table-wrapper">
            <table v-if="products.length">
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th>Precio</th>
                  <th>Categorías</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="product in products" :key="product.id">
                  <td>{{ product.name }}</td>
                  <td>${{ Number(product.price).toFixed(2) }}</td>
                  <td>{{ product.category?.join(', ') }}</td>
                  <td>
                    <button class="secondary small" @click="editProduct(product)">Editar</button>
                    <button class="danger small" @click="deleteProduct(product)">Eliminar</button>
                    <button class="primary small" @click="addToCart(product)">Agregar al carrito</button>
                  </td>
                </tr>
              </tbody>
            </table>

            <p v-else-if="!loading" class="empty">No hay productos para mostrar.</p>
            <p v-else class="empty">Cargando productos...</p>
          </div>

          <div class="pagination" v-if="totalCount">
            <button :disabled="pageIndex === 1" @click="goToPage(pageIndex - 1)">Anterior</button>
            <span>Página {{ pageIndex }} de {{ totalPages }}</span>
            <button :disabled="pageIndex === totalPages" @click="goToPage(pageIndex + 1)">Siguiente</button>
          </div>
        </section>
      </div>

      <!-- Panel deslizable del carrito -->
      <div v-if="cartOpen" class="cart-overlay" @click="cartOpen = false"></div>
      <aside class="cart-drawer" :class="{ open: cartOpen }">
        <div class="cart-drawer-header">
          <h2>Carrito</h2>
          <button class="close-btn" @click="cartOpen = false">✕</button>
        </div>

        <div v-if="basketError" class="alert">{{ basketError }}</div>

        <div class="toolbar">
          <input v-model="basketUserName" placeholder="Usuario" @keyup.enter="loadBasket" />
          <button class="primary" @click="loadBasket">Cargar</button>
        </div>

        <div v-if="basket">
          <div class="table-wrapper">
            <table v-if="basket.items.length">
              <thead>
                <tr>
                  <th>Producto</th>
                  <th>Cant.</th>
                  <th>Precio</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in basket.items" :key="item.productId">
                  <td>{{ item.productName }}</td>
                  <td>{{ item.quantity }}</td>
                  <td>${{ Number(item.price).toFixed(2) }}</td>
                  <td>
                    <button class="danger small" @click="removeFromCart(item)">Quitar</button>
                  </td>
                </tr>
              </tbody>
            </table>
            <p v-else class="empty">El carrito está vacío.</p>
          </div>

          <div class="cart-total">Total: ${{ basketTotal.toFixed(2) }}</div>

          <div class="actions">
            <button class="secondary" @click="clearBasket">Vaciar carrito</button>
            <button class="primary" :disabled="checkoutLoading || !basket.items.length" @click="realizarCompra">
              {{ checkoutLoading ? 'Procesando...' : 'Realizar compra' }}
            </button>
          </div>

          <div v-if="checkoutError" class="alert">{{ checkoutError }}</div>
        </div>
        <p v-else-if="!basketLoading" class="empty">Carga un usuario para ver su carrito.</p>
      </aside>
    </section>

    <!-- ===================== VISTA: TODAS LAS ORDENES ===================== -->
    <section v-else-if="currentView === 'orders'" class="panel">
      <div class="header">
        <div>
          <p class="eyebrow">Ejemplo web</p>
          <h1>Pedidos</h1>
          <p class="subtitle">Todas las órdenes de compra, o filtradas por cliente.</p>
        </div>
      </div>

      <div v-if="ordersError" class="alert">{{ ordersError }}</div>

      <section class="card list-card">
        <div class="toolbar">
          <input v-model="ordersCustomerFilter" placeholder="Filtrar por cliente" @keyup.enter="loadOrders" />
          <button class="primary" @click="loadOrders">Buscar</button>
          <button class="secondary" @click="clearOrdersFilter">Ver todas</button>
        </div>

        <div class="table-wrapper">
          <table v-if="orders.length">
            <thead>
              <tr>
                <th>Orden</th>
                <th>Cliente</th>
                <th>Fecha</th>
                <th>Hora</th>
                <th>Productos</th>
                <th>Total</th>
                <th>Estado</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="order in orders" :key="order.id" class="row-clickable" @click="openOrderPage(order.id)">
                <td class="mono">{{ order.id.slice(0, 8) }}...</td>
                <td>{{ order.customerId }}</td>
                <td>{{ formatDate(order.createdAt) }}</td>
                <td>{{ formatTime(order.createdAt) }}</td>
                <td>{{ summarizeProducts(order) }}</td>
                <td>${{ Number(order.total).toFixed(2) }}</td>
                <td><span class="status-badge" :class="order.status.toLowerCase()">{{ order.status }}</span></td>
                <td @click.stop>
                  <button class="secondary small" @click="openOrderPage(order.id)">Ver detalle</button>
                </td>
              </tr>
            </tbody>
          </table>
          <p v-else-if="!ordersLoading" class="empty">No hay órdenes para mostrar.</p>
          <p v-else class="empty">Cargando órdenes...</p>
        </div>
      </section>
    </section>

    <!-- ===================== VISTA: DETALLE DE ORDEN ===================== -->
    <section v-else-if="currentView === 'order'" class="panel">
      <div class="header">
        <div>
          <p class="eyebrow">Ejemplo web</p>
          <h1>Detalle de orden</h1>
        </div>
      </div>

      <div v-if="orderDetailError" class="alert">{{ orderDetailError }}</div>
      <p v-if="orderDetailLoading" class="empty">Cargando orden...</p>

      <section v-if="orderDetail" class="card">
        <dl class="summary-grid">
          <div>
            <dt>Orden</dt>
            <dd class="mono">{{ orderDetail.id }}</dd>
          </div>
          <div>
            <dt>Cliente</dt>
            <dd>{{ orderDetail.customerId }}</dd>
          </div>
          <div>
            <dt>Fecha</dt>
            <dd>{{ formatDate(orderDetail.createdAt) }}</dd>
          </div>
          <div>
            <dt>Hora</dt>
            <dd>{{ formatTime(orderDetail.createdAt) }}</dd>
          </div>
          <div>
            <dt>Estado</dt>
            <dd><span class="status-badge" :class="orderDetail.status.toLowerCase()">{{ orderDetail.status }}</span></dd>
          </div>
        </dl>

        <div class="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Producto</th>
                <th>Cantidad</th>
                <th>Precio unitario</th>
                <th>Total línea</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in orderDetail.items" :key="item.productId">
                <td>{{ item.productName }}</td>
                <td>{{ item.quantity }}</td>
                <td>${{ Number(item.unitPrice).toFixed(2) }}</td>
                <td>${{ Number(item.lineTotal).toFixed(2) }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="totals-row">
          <span>Subtotal: ${{ Number(orderDetail.subtotal).toFixed(2) }}</span>
          <span>Impuesto: ${{ Number(orderDetail.tax).toFixed(2) }}</span>
          <span class="totals-final">Total: ${{ Number(orderDetail.total).toFixed(2) }}</span>
        </div>

        <div class="actions">
          <button
            v-if="orderDetail.status === 'Pending'"
            class="primary"
            @click="changeOrderDetailStatus('Confirmed')"
          >Confirmar orden</button>
          <button
            v-if="orderDetail.status === 'Pending'"
            class="danger"
            @click="changeOrderDetailStatus('Cancelled')"
          >Cancelar orden</button>
          <button class="secondary" @click="descargarPdf(orderDetail)">📄 Descargar PDF</button>
        </div>
      </section>
    </section>
  </main>
</template>
