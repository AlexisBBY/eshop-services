<script setup>
import { computed, onMounted, ref } from 'vue'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5201'

const products = ref([])
const loading = ref(false)
const error = ref('')
const searchTerm = ref('')
const pageIndex = ref(1)
const pageSize = ref(5)
const totalCount = ref(0)
const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

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

    if (searchTerm.value.trim()) {
      params.set('name', searchTerm.value.trim())
    }

    const response = await fetch(`${API_URL}/products?${params.toString()}`)
    if (!response.ok) {
      throw new Error('No se pudieron cargar los productos')
    }

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
  form.value = {
    id: null,
    name: '',
    description: '',
    category: '',
    imageFile: '',
    price: ''
  }
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

    const url = editingId.value ? `${API_URL}/products` : `${API_URL}/products`
    const method = editingId.value ? 'PUT' : 'POST'

    if (editingId.value) {
      payload.id = form.value.id
    }

    const response = await fetch(url, {
      method,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })

    if (!response.ok) {
      throw new Error('No se pudo guardar el producto')
    }

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
    const response = await fetch(`${API_URL}/products/by-name/${encodeURIComponent(product.name)}`, {
      method: 'DELETE'
    })

    if (!response.ok) {
      throw new Error('No se pudo eliminar')
    }

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

function searchProducts() {
  pageIndex.value = 1
  loadProducts()
}

onMounted(() => {
  loadProducts()
})
</script>

<template>
  <main class="page">
    <section class="panel">
      <div class="header">
        <div>
          <p class="eyebrow">Ejemplo web</p>
          <h1>Catálogo de productos</h1>
          <p class="subtitle">Busca, crea, actualiza y elimina productos usando la API del proyecto.</p>
        </div>
        <div class="badge">Vue + Vite</div>
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
            <input v-model="searchTerm" placeholder="Buscar por nombre" @keyup.enter="searchProducts" />
            <button class="primary" @click="searchProducts">Buscar</button>
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
    </section>
  </main>
</template>
